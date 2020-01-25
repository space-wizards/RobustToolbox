using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Animations;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
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
        private EntityUid _parent;
        private Vector2 _localPosition; // holds offset from grid, or offset from parent
        private Angle _localRotation; // local rotation
        private GridId _gridID;

        private Matrix3 _worldMatrix = Matrix3.Identity;
        private Matrix3 _invWorldMatrix = Matrix3.Identity;

        private Vector2 _nextPosition;
        private Angle _nextRotation;

        [ViewVariables] private readonly List<EntityUid> _children = new List<EntityUid>();

#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IGameTiming _gameTiming;
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

        /// <inheritdoc />
        public event EventHandler<MoveEventArgs> OnMove;

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
            private set
            {
                var entMessage = new EntParentChangedMessage(Owner, Parent?.Owner);
                var compMessage = new ParentChangedMessage(value?.Owner, Parent?.Owner);
                _parent = value?.Owner.Uid ?? EntityUid.Invalid;
                Owner.EntityManager.EventBus.RaiseEvent(Owner, entMessage);
                Owner.SendMessage(this, compMessage);
            }
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

                Dirty();

                //TODO: This is a hack, look into WHY we can't call GridPosition before the comp is Running
                if (Running)
                {
                    RebuildMatrices();
                    OnMove?.Invoke(this, new MoveEventArgs(GridPosition, value));
                }
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
                Dirty();

                RebuildMatrices();
                OnMove?.Invoke(this, new MoveEventArgs(GridPosition, new GridCoordinates(GetLocalPosition(), GridID)));
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
                OnMove?.Invoke(this, new MoveEventArgs(oldPos, GridPosition));
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
        }

        /// <inheritdoc />
        protected override void Startup()
        {
            base.Startup();

            // Keep the cached matrices in sync with the fields.
            RebuildMatrices();
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
                var concrete = (TransformComponent) Parent;
                concrete._children.Remove(Owner.Uid);

                // detach
                Parent = null;
            }

            base.OnRemove();
        }

        /// <summary>
        /// Detaches this entity from its parent.
        /// </summary>
        public virtual void DetachParent()
        {
            var mapPos = MapPosition;

            // nothing to do
            if (Parent == null)
                return;

            var newMapEntity = _mapManager.GetMapEntity(mapPos.MapId);

            // this would be a no-op
            if (newMapEntity == Parent.Owner)
                return;

            var concrete = (TransformComponent) Parent;
            concrete._children.Remove(Owner.Uid);

            // attach to map
            Parent = newMapEntity.Transform;
            MapPosition = mapPos;


            Dirty();
        }

        /// <summary>
        /// Sets another entity as the parent entity.
        /// </summary>
        /// <param name="parent"></param>
        public virtual void AttachParent(ITransformComponent parent)
        {
            //NOTE: This function must be callable from before initialize

            // nothing to attach to.
            if (parent == null)
                return;

            // That's already our parent, don't bother attaching again.
            if (parent.Owner.Uid == _parent)
                return;

            var oldConcrete = (TransformComponent) Parent;
            oldConcrete?._children.Remove(Owner.Uid);
            var newConcrete = (TransformComponent) parent;
            newConcrete._children.Add(Owner.Uid);
            Parent = parent;

            // offset position from world to parent
            SetPosition(parent.InvWorldMatrix.Transform(GetLocalPosition()));
            RebuildMatrices();
            Dirty();
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

                    OnMove?.Invoke(this, new MoveEventArgs(oldPos, GridPosition));
                    rebuildMatrices = true;
                }

                if (rebuildMatrices)
                {
                    RebuildMatrices();
                }
            }

            if (nextState != null)
                _nextPosition = ((TransformComponentState) nextState).LocalPosition;
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
