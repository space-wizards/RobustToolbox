using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Animations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects.Components.Transform
{
    internal class TransformComponent : Component, ITransformComponent, IComponentDebug
    {
        // Max distance per tick how far an entity can move before it is considered teleporting.
        private const float MaxInterpDistance = 2.0f;

        private EntityUid _parent;
        private Vector2 _localPosition; // holds offset from grid, or offset from parent
        private Angle _localRotation; // local rotation
        private GridId _gridID;

        private Matrix3 _worldMatrix = Matrix3.Identity;
        private Matrix3 _invWorldMatrix = Matrix3.Identity;

        private Vector2 _nextPosition;
        private Angle _nextRotation;

        [ViewVariables] private readonly SortedSet<EntityUid> _children = new SortedSet<EntityUid>();

#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IGameTiming _gameTiming;
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

        /// <inheritdoc />
        public override string Name => "Transform";

        /// <inheritdoc />
        public sealed override uint? NetID => NetIDs.TRANSFORM;

        /// <inheritdoc />
        [ViewVariables]
        public MapId MapID
        {
            get
            {
                // branch or leaf node
                if (_parent.IsValid())
                    return Parent.MapID;

                // root node, expected to have map component
                if (Owner.TryGetComponent(out IMapComponent mapComp))
                    return mapComp.WorldMap;

                throw new InvalidOperationException("Transform node does not exist inside scene tree!");
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        public GridId GridID
        {
            get
            {
                // root node, grid id is undefined
                if (Owner.HasComponent<IMapComponent>())
                    return GridId.Invalid;

                // second level node, terminates recursion up the branch of the tree
                if (Owner.TryGetComponent(out IMapGridComponent gridComp))
                    return gridComp.GridIndex;

                // branch or leaf node
                if (_parent.IsValid())
                    return Parent.GridID;

                // Not on a grid
                return GridId.Invalid;
            }
        }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public Angle LocalRotation
        {
            get => GetLocalRotation();
            set
            {
                if (GetLocalRotation() == value)
                    return;

                SetRotation(value);
                RebuildMatrices();
                Dirty();
                UpdateEntityTree();
                UpdatePhysicsTree();
            }
        }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public Angle WorldRotation
        {
            get
            {
                if (_parent.IsValid())
                {
                    return Parent.WorldRotation + GetLocalRotation();
                }

                return GetLocalRotation();
            }
            set
            {
                var current = WorldRotation;
                var diff = value - current;
                LocalRotation += diff;
            }
        }

        /// <summary>
        ///     Current parent entity of this entity.
        /// </summary>
        [ViewVariables]
        public ITransformComponent Parent
        {
            get => !_parent.IsValid() ? null : Owner.EntityManager.GetEntity(_parent).Transform;
            set
            {
                if (value != null)
                {
                    AttachParent(value);
                }
                else
                {
                    DetachParent();
                }
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public EntityUid ParentUid
        {
            get => _parent;
            set => Parent = _entityManager.GetEntity(value).Transform;
        }

        /// <inheritdoc />
        public Matrix3 WorldMatrix
        {
            get
            {
                if (_parent.IsValid())
                {
                    var parentMatrix = Parent.WorldMatrix;
                    var myMatrix = GetLocalMatrix();
                    Matrix3.Multiply(ref myMatrix, ref parentMatrix, out var result);
                    return result;
                }

                return GetLocalMatrix();
            }
        }

        /// <inheritdoc />
        public Matrix3 InvWorldMatrix
        {
            get
            {
                if (_parent.IsValid())
                {
                    var matP = Parent.InvWorldMatrix;
                    var myMatrix = GetLocalMatrixInv();
                    Matrix3.Multiply(ref matP, ref myMatrix, out var result);
                    return result;
                }

                return GetLocalMatrixInv();
            }
        }

        public bool IsMapTransform => !ContainerHelpers.IsInContainer(Owner);

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public GridCoordinates GridPosition
        {
            get
            {
                if (_parent.IsValid())
                {
                    // transform _position from parent coords to world coords
                    var worldPos = Parent.WorldMatrix.Transform(GetLocalPosition());
                    return new GridCoordinates(worldPos, GridID);
                }
                else
                {
                    return new GridCoordinates(GetLocalPosition(), GridID);
                }
            }
            set
            {
                if (!_parent.IsValid())
                {
                    DebugTools.Assert("Tried to move root node.");
                    return;
                }

                // grid coords to world coords
                var worldCoords = value.ToMapPos(_mapManager);

                if (value.GridID != GridID)
                {
                    var newGrid = _mapManager.GetGrid(value.GridID);
                    AttachParent(_entityManager.GetEntity(newGrid.GridEntityId));
                }

                // world coords to parent coords
                var newPos = Parent.InvWorldMatrix.Transform(worldCoords);

                // float rounding error guard, if the offset is less than 1mm ignore it
                if ((newPos - GetLocalPosition()).LengthSquared < 10.0E-3)
                    return;

                SetPosition(newPos);

                //TODO: This is a hack, look into WHY we can't call GridPosition before the comp is Running
                if (Running)
                {
                    RebuildMatrices();
                    Owner.SendMessage(this, new MoveMessage(GridPosition, value));
                }

                Dirty();
                UpdateEntityTree();
                UpdatePhysicsTree();
            }
        }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 WorldPosition
        {
            get
            {
                if (_parent.IsValid())
                {
                    // parent coords to world coords
                    return Parent.WorldMatrix.Transform(GetLocalPosition());
                }
                else
                {
                    return Vector2.Zero;
                }
            }
            set
            {
                if (!_parent.IsValid())
                {
                    DebugTools.Assert("Tried to move root node.");
                    return;
                }

                // world coords to parent coords
                var newPos = Parent.InvWorldMatrix.Transform(value);

                // float rounding error guard, if the offset is less than 1mm ignore it
                if ((newPos - GetLocalPosition()).LengthSquared < 1.0E-3)
                    return;

                if (_localPosition == newPos)
                    return;

                SetPosition(newPos);

                RebuildMatrices();
                Dirty();
                UpdateEntityTree();
                UpdatePhysicsTree();

                Owner.SendMessage(this, new MoveMessage(GridPosition, new GridCoordinates(GetLocalPosition(), GridID)));
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public MapCoordinates MapPosition
        {
            get => new MapCoordinates(WorldPosition, MapID);
            set
            {
                AttachParent(_mapManager.GetMapEntity(value.MapId));

                WorldPosition = value.Position;
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public Vector2 LocalPosition
        {
            get => GetLocalPosition();
            set
            {
                var oldPos = GridPosition;
                SetPosition(value);
                RebuildMatrices();
                Dirty();
                UpdateEntityTree();
                UpdatePhysicsTree();
                Owner.SendMessage(this, new MoveMessage(oldPos, GridPosition));
            }
        }

        [ViewVariables]
        public IEnumerable<ITransformComponent> Children =>
            _children.Select(u => Owner.EntityManager.GetEntity(u).Transform);

        public IEnumerable<EntityUid> ChildEntityUids => _children;

        public int ChildCount => _children.Count;

        /// <inheritdoc />
        public Vector2 LerpDestination => _nextPosition;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            // Verifies MapID can be resolved.
            // If it cannot, then this is an orphan entity, and an exception will be thrown.
            // DO NOT REMOVE THIS LINE
            var _ = MapID;

            UpdateEntityTree();
        }

        /// <inheritdoc />
        protected override void Startup()
        {
            base.Startup();

            // Keep the cached matrices in sync with the fields.
            RebuildMatrices();
            Dirty();
            UpdateEntityTree();
        }

        /// <inheritdoc />
        public override void OnRemove()
        {
            // DeleteEntity modifies our _children collection, we must cache the collection to iterate properly
            foreach (var childUid in _children.ToArray())
            {
                // Recursion: DeleteEntity calls the Transform.OnRemove function of child entities.
                Owner.EntityManager.DeleteEntity(childUid);
            }

            // map does not have a parent node
            if (Parent != null)
            {
                DetachParentToNull();
            }

            base.OnRemove();
        }

        /// <summary>
        /// Detaches this entity from its parent.
        /// </summary>
        public void DetachParent()
        {
            // nothing to do
            var oldParent = Parent;
            if (oldParent == null)
            {
                return;
            }

            var mapPos = MapPosition;
            var newMapEntity = _mapManager.GetMapEntity(mapPos.MapId);

            // this would be a no-op
            var oldParentEnt = oldParent.Owner;
            if (newMapEntity == oldParentEnt)
            {
                return;
            }

            AttachParent(newMapEntity);

            MapPosition = mapPos;

            Dirty();
        }

        private void DetachParentToNull()
        {
            var oldParent = Parent;
            if (oldParent == null)
            {
                return;
            }

            var oldConcrete = (TransformComponent) oldParent;
            var uid = Owner.Uid;
            oldConcrete._children.Remove(uid);

            var oldParentOwner = oldParent?.Owner;

            var entMessage = new EntParentChangedMessage(Owner, oldParentOwner);
            var compMessage = new ParentChangedMessage(null, oldParentOwner);
            _parent = EntityUid.Invalid;
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, entMessage);
            Owner.SendMessage(this, compMessage);

            // Does it even make sense to call these since this is called purely from OnRemove right now?
            RebuildMatrices();
            Dirty();
        }

        /// <summary>
        /// Sets another entity as the parent entity.
        /// </summary>
        /// <param name="newParent"></param>
        public virtual void AttachParent(ITransformComponent newParent)
        {
            //NOTE: This function must be callable from before initialize

            // nothing to attach to.
            if (newParent == null)
                return;

            // That's already our parent, don't bother attaching again.
            var newParentEnt = newParent.Owner;
            if (newParentEnt.Uid == _parent)
            {
                return;
            }

            var oldParent = Parent;
            var oldConcrete = (TransformComponent) oldParent;
            var uid = Owner.Uid;
            oldConcrete?._children.Remove(uid);
            var newConcrete = (TransformComponent) newParent;
            newConcrete._children.Add(uid);

            var oldParentOwner = oldParent?.Owner;
            var entMessage = new EntParentChangedMessage(Owner, oldParentOwner);
            var compMessage = new ParentChangedMessage(newParentEnt, oldParentOwner);
            _parent = newParentEnt.Uid;
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, entMessage);
            Owner.SendMessage(this, compMessage);

            // offset position from world to parent
            SetPosition(newParent.InvWorldMatrix.Transform(GetLocalPosition()));
            RebuildMatrices();
            Dirty();
            UpdateEntityTree();
        }

        public void AttachParent(IEntity parent)
        {
            var transform = parent.Transform;
            AttachParent(transform);
        }

        /// <summary>
        ///     Finds the transform of the entity located on the map itself
        /// </summary>
        public ITransformComponent GetMapTransform()
        {
            if (Parent != null) //If we are not the final transform, query up the chain of parents
            {
                return Parent.GetMapTransform();
            }

            return this;
        }


        /// <summary>
        ///     Does this entity contain the entity in the argument
        /// </summary>
        public bool ContainsEntity(ITransformComponent entityTransform)
        {
            if (entityTransform.Parent == null) //Is the entity the scene root
            {
                return false;
            }

            if (this == entityTransform.Parent) //Is this the direct container of the entity
            {
                return true;
            }
            else
            {
                return
                    ContainsEntity(entityTransform
                        .Parent); //Recursively search up the entities containers for this object
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _parent, "parent", new EntityUid());
            serializer.DataField(ref _localPosition, "pos", Vector2.Zero);
            serializer.DataField(ref _localRotation, "rot", new Angle());

            if (serializer.Reading && serializer.TryReadDataField("grid", out GridId grid))
            {
                _gridID = grid;
            }
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new TransformComponentState(_localPosition, LocalRotation, Parent?.Owner?.Uid);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (curState != null)
            {
                var newState = (TransformComponentState) curState;

                var newParentId = newState.ParentID;
                var rebuildMatrices = false;
                if (Parent?.Owner?.Uid != newParentId)
                {
                    if (newParentId.HasValue && newParentId.Value.IsValid())
                    {
                        var newParent = Owner.EntityManager.GetEntity(newParentId.Value);
                        AttachParent(newParent.Transform);
                    }

                    rebuildMatrices = true;
                }

                if (LocalRotation != newState.Rotation)
                {
                    SetRotation(newState.Rotation);
                    rebuildMatrices = true;
                }

                if (_localPosition != newState.LocalPosition)
                {
                    var oldPos = GridPosition;
                    if (_localPosition != newState.LocalPosition)
                    {
                        SetPosition(newState.LocalPosition);
                    }

                    Owner.SendMessage(this, new MoveMessage(oldPos, GridPosition));
                    rebuildMatrices = true;
                }

                if (rebuildMatrices)
                {
                    RebuildMatrices();
                }

                Dirty();
                UpdateEntityTree();
                TryUpdatePhysicsTree();
            }

            if (nextState != null)
            {
                var nextPos = ((TransformComponentState) nextState).LocalPosition;

                if ((nextPos - _localPosition).LengthSquared < 2.0f)
                    _nextPosition = nextPos;
                else
                    _nextPosition = _localPosition;
            }
            else
                _nextPosition = _localPosition; // this should cause the lerp to do nothing

            if (nextState != null)
                _nextRotation = ((TransformComponentState) nextState).Rotation;
            else
                _nextRotation = _localRotation; // this should cause the lerp to do nothing
        }

        // Hooks for GodotTransformComponent go here.
        protected virtual void SetPosition(Vector2 position)
        {
            _localPosition = position;
        }

        protected virtual void SetRotation(Angle rotation)
        {
            _localRotation = rotation;
        }

        protected virtual Vector2 GetLocalPosition()
        {
            if (_gameTiming.InSimulation || _localPosition == _nextPosition || Owner.Uid.IsClientSide())
                return _localPosition;

            return Vector2.Lerp(_localPosition, _nextPosition,
                (float) (_gameTiming.TickRemainder.TotalSeconds / _gameTiming.TickPeriod.TotalSeconds));
        }

        protected virtual Angle GetLocalRotation()
        {
            if (_gameTiming.InSimulation || _localRotation == _nextRotation || Owner.Uid.IsClientSide())
                return _localRotation;

            return Angle.Lerp(_localRotation, _nextRotation,
                (float) (_gameTiming.TickRemainder.TotalSeconds / _gameTiming.TickPeriod.TotalSeconds));
        }

        public Matrix3 GetLocalMatrix()
        {
            if (_gameTiming.InSimulation || Owner.Uid.IsClientSide())
                return _worldMatrix;

            // there really is no point trying to cache this because it will only be used in one frame
            var pos = GetLocalPosition();
            var rot = GetLocalRotation().Theta;

            var posMat = Matrix3.CreateTranslation(pos);
            var rotMat = Matrix3.CreateRotation((float) rot);

            Matrix3.Multiply(ref rotMat, ref posMat, out var transMat);

            return transMat;
        }

        public Matrix3 GetLocalMatrixInv()
        {
            if (_gameTiming.InSimulation || Owner.Uid.IsClientSide())
                return _invWorldMatrix;

            // there really is no point trying to cache this because it will only be used in one frame
            var pos = GetLocalPosition();
            var rot = GetLocalRotation().Theta;

            var posMat = Matrix3.CreateTranslation(pos);
            var rotMat = Matrix3.CreateRotation((float) rot);
            var posImat = Matrix3.Invert(posMat);
            var rotImap = Matrix3.Invert(rotMat);

            Matrix3.Multiply(ref posImat, ref rotImap, out var itransMat);

            return itransMat;
        }

        private void RebuildMatrices()
        {
            var pos = _localPosition;

            if (!_parent.IsValid()) // Root Node
                pos = Vector2.Zero;

            var rot = _localRotation.Theta;

            var posMat = Matrix3.CreateTranslation(pos);
            var rotMat = Matrix3.CreateRotation((float) rot);

            Matrix3.Multiply(ref rotMat, ref posMat, out var transMat);

            _worldMatrix = transMat;

            var posImat = Matrix3.Invert(posMat);
            var rotImap = Matrix3.Invert(rotMat);

            Matrix3.Multiply(ref posImat, ref rotImap, out var itransMat);

            _invWorldMatrix = itransMat;
        }

        private bool TryUpdatePhysicsTree() => Initialized && UpdatePhysicsTree();

        private bool UpdatePhysicsTree() =>
            Owner.TryGetComponent(out ICollidableComponent collider) && collider.UpdatePhysicsTree();

        private bool UpdateEntityTree() => _entityManager.UpdateEntityTree(Owner);

        public string GetDebugString()
        {
            return $"pos/rot/wpos/wrot: {GridPosition}/{LocalRotation}/{WorldPosition}/{WorldRotation}";
        }

        /// <summary>
        ///     Serialized state of a TransformComponent.
        /// </summary>
        [Serializable, NetSerializable]
        protected internal class TransformComponentState : ComponentState
        {
            /// <summary>
            ///     Current parent entity of this entity.
            /// </summary>
            public readonly EntityUid? ParentID;

            /// <summary>
            ///     Current position offset of the entity.
            /// </summary>
            public readonly Vector2 LocalPosition;

            /// <summary>
            ///     Current rotation offset of the entity.
            /// </summary>
            public readonly Angle Rotation;

            /// <summary>
            ///     Constructs a new state snapshot of a TransformComponent.
            /// </summary>
            /// <param name="localPosition">Current position offset of this entity.</param>
            /// <param name="rotation">Current direction offset of this entity.</param>
            /// <param name="parentId">Current parent transform of this entity.</param>
            public TransformComponentState(Vector2 localPosition, Angle rotation, EntityUid? parentId)
                : base(NetIDs.TRANSFORM)
            {
                LocalPosition = localPosition;
                Rotation = rotation;
                ParentID = parentId;
            }
        }
    }
}
