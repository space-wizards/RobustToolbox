﻿using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects.EntitySystemMessages;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Map;
 using SS14.Shared.Interfaces.Timing;
 using SS14.Shared.IoC;
 using SS14.Shared.Log;
 using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Serialization;
 using SS14.Shared.Utility;
 using SS14.Shared.ViewVariables;

namespace SS14.Shared.GameObjects.Components.Transform
{
    internal class TransformComponent : Component, ITransformComponent, IComponentDebug
    {
        private EntityUid _parent;
        private Vector2 _localPosition; // holds offset from grid, or offset from parent
        private Angle _localRotation; // local rotation
        private GridId _gridID;

        private Matrix3 _worldMatrix;
        private Matrix3 _invWorldMatrix;

        private Vector2 _nextPosition;
        private Angle _nextRotation;

        [ViewVariables]
        private readonly List<EntityUid> _children = new List<EntityUid>();

        /// <inheritdoc />
        public event EventHandler<MoveEventArgs> OnMove;

        public event Action<ParentChangedEventArgs> OnParentChanged;
        
        /// <inheritdoc />
        public override string Name => "Transform";
        /// <inheritdoc />
        public sealed override uint? NetID => NetIDs.TRANSFORM;
        /// <inheritdoc />
        public sealed override Type StateType => typeof(TransformComponentState);

        /// <inheritdoc />
        [ViewVariables]
        public MapId MapID
        {
            get
            {
                // Work around a client-side race condition of the grids not being synced yet.
                // Maybe it's better to fix the race condition instead.
                // Eh.
                if (IoCManager.Resolve<IMapManager>().TryGetGrid(GridID, out var grid))
                {
                    return grid.MapID;
                }
                return MapId.Nullspace;
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        public GridId GridID
        {
            get => _parent.IsValid() ? Parent.GridID : _gridID;
        }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public Angle LocalRotation
        {
            get => GetLocalRotation();
            set
            {
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
                var old = _parent;
                var msg = new EntParentChangedMessage(Owner, Parent?.Owner);
                _parent = value?.Owner.Uid ?? EntityUid.Invalid;
                Owner.EntityManager.RaiseEvent(Owner, msg);
                OnParentChanged?.Invoke(new ParentChangedEventArgs(old, _parent));
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
                    var myMatrix = GetWorldMatrix();
                    Matrix3.Multiply(ref myMatrix, ref parentMatrix, out var result);
                    return result;
                }
                return GetWorldMatrix();
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
                    var myMatrix = GetWorldMatrixInv();
                    Matrix3.Multiply(ref matP, ref myMatrix, out var result);
                    return result;
                }
                return GetWorldMatrixInv();
            }
        }

        public bool IsMapTransform => Parent == null;


        public virtual bool VisibleWhileParented { set; get; }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public GridCoordinates GridPosition
        {
            get
            {
                if (Parent != null)
                {
                    // transform _position from parent coords to world coords
                    var worldPos = Parent.WorldMatrix.Transform(GetLocalPosition());
                    return new GridCoordinates(worldPos, _gridID);
                }
                else
                {
                    return new GridCoordinates(GetLocalPosition(), _gridID);
                }
            }
            set
            {
                if (_parent.IsValid())
                {
                    if (value.GridID != _gridID)
                    {
                        throw new ArgumentException("Cannot change grid ID of parented entity.");
                    }
                    // grid coords to world coords
                    var worldCoords = value.ToWorld();

                    // world coords to parent coords
                    var newPos = Parent.InvWorldMatrix.Transform(worldCoords.Position);

                    // float rounding error guard, if the offset is less than 1mm ignore it
                    if ((newPos - GetLocalPosition()).LengthSquared < 10.0E-3)
                        return;

                    SetPosition(newPos);
                }
                else
                {
                    SetPosition(value.Position);

                    _recurseSetGridId(value.GridID);
                }

                Dirty();

                RebuildMatrices();
                OnMove?.Invoke(this, new MoveEventArgs(GridPosition, value));
            }
        }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 WorldPosition
        {
            get
            {
                if (Parent != null)
                {
                    // parent coords to world coords
                    return Parent.WorldMatrix.Transform(GetLocalPosition());
                }
                else
                {
                    // Work around a client-side race condition of the grids not being synced yet.
                    // Maybe it's better to fix the race condition instead.
                    // Eh.
                    if (IoCManager.Resolve<IMapManager>().TryGetGrid(GridID, out var grid))
                    {
                        return grid.ConvertToWorld(GetLocalPosition());
                    }
                    return GetLocalPosition();
                }
            }
            set
            {
                if (_parent.IsValid())
                {
                    // world coords to parent coords
                    var newPos = Parent.InvWorldMatrix.Transform(value);

                    // float rounding error guard, if the offset is less than 1mm ignore it
                    if ((newPos - GetLocalPosition()).LengthSquared < 10.0E-3)
                        return;

                    SetPosition(newPos);
                }
                else
                {
                    SetPosition(value);
                    _recurseSetGridId(IoCManager.Resolve<IMapManager>().GetMap(MapID).FindGridAt(GetLocalPosition()).Index);
                }

                Dirty();

                RebuildMatrices();
                OnMove?.Invoke(this, new MoveEventArgs(GridPosition, new GridCoordinates(GetLocalPosition(), GridID)));
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public MapCoordinates MapPosition => new MapCoordinates(WorldPosition, MapID);

        [ViewVariables(VVAccess.ReadWrite)]
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
        public IEnumerable<ITransformComponent> Children => _children.Select(u => Owner.EntityManager.GetEntity(u).Transform);

        /// <inheritdoc />
        public Vector2 LerpDestination => _nextPosition;

        /// <inheritdoc />
        public override void OnRemove()
        {
            DetachParent();

            foreach (var child in _children.ToArray())
            {
                var transform = Owner.EntityManager.GetEntity(child).Transform;
                transform.DetachParent();
                transform.GridPosition = GridCoordinates.Nullspace;
            }

            base.OnRemove();
        }

        /// <summary>
        /// Detaches this entity from its parent.
        /// </summary>
        public virtual void DetachParent()
        {
            // nothing to do
            if (Parent == null)
                return;

            // transform _position from parent coords to world coords
            var localPosition = GridPosition;

            var concrete = (TransformComponent) Parent;
            concrete._children.Remove(Owner.Uid);

            // detach
            Parent = null;

            // switch position back to grid coords
            SetPosition(localPosition.Position);

            Dirty();
        }

        /// <summary>
        /// Sets another entity as the parent entity.
        /// </summary>
        /// <param name="parent"></param>
        public virtual void AttachParent(ITransformComponent parent)
        {
            // nothing to attach to.
            if (parent == null)
                return;

            var oldConcrete = (TransformComponent) Parent;
            oldConcrete?._children.Remove(Owner.Uid);
            var newConcrete = (TransformComponent) parent;
            newConcrete._children.Add(Owner.Uid);
            Parent = parent;

            // move to parents grid
            _recurseSetGridId(parent.GridID);

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
            if (entityTransform.IsMapTransform) //Is the entity on the map
            {
                return false;
            }

            if (this == entityTransform.Parent) //Is this the direct container of the entity
            {
                return true;
            }
            else
            {
                return ContainsEntity(entityTransform.Parent); //Recursively search up the entities containers for this object
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _parent, "parent", new EntityUid());
            serializer.DataField(ref _gridID, "grid", GridId.Nullspace);
            serializer.DataField(ref _localPosition, "pos", Vector2.Zero);
            serializer.DataField(ref _localRotation, "rot", new Angle());
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new TransformComponentState(_localPosition, GridID, LocalRotation, Parent?.Owner?.Uid);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            var curTrans = curState as TransformComponentState;
            var nextTrans = nextState as TransformComponentState;

            Logger.DebugS("ent", $"cTick={(curTrans != null? curTrans.LocalPosition.ToString():"-")}, nTick={(nextTrans!=null?nextTrans.LocalPosition.ToString():"-")}");

            if (curState != null)
            {
                var newState = (TransformComponentState) curState;

                var newParentId = newState.ParentID;
                var rebuildMatrices = false;
                if (Parent?.Owner?.Uid != newParentId)
                {
                    DetachParent();

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

                if (_localPosition != newState.LocalPosition || (!_parent.IsValid() && GridID != newState.GridID))
                {
                    var oldPos = GridPosition;
                    if (_localPosition != newState.LocalPosition)
                    {
                        SetPosition(newState.LocalPosition);
                    }

                    if (!_parent.IsValid() && GridID != newState.GridID)
                    {
                        _recurseSetGridId(newState.GridID);
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
                _nextRotation = ((TransformComponentState)nextState).Rotation;
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
            IGameTiming timing = IoCManager.Resolve<IGameTiming>();
            if(timing.InSimulation || _localPosition == _nextPosition)
                return _localPosition;

            return Vector2.Lerp(_localPosition, _nextPosition, (float) (timing.TickRemainder.TotalSeconds / timing.TickPeriod.TotalSeconds));
        }

        protected virtual Angle GetLocalRotation()
        {
            IGameTiming timing = IoCManager.Resolve<IGameTiming>();
            if (timing.InSimulation || _localRotation == _nextRotation)
                return _localRotation;

            return Angle.Lerp(_localRotation, _nextRotation, (float)(timing.TickRemainder.TotalSeconds / timing.TickPeriod.TotalSeconds));
        }

        protected virtual Matrix3 GetWorldMatrix()
        {
            IGameTiming timing = IoCManager.Resolve<IGameTiming>();
            if (timing.InSimulation)
                return _worldMatrix;

            // there really is no point trying to cache this because it will only be used in one frame
            var pos = GetLocalPosition();
            var rot = GetLocalRotation().Theta;

            var posMat = Matrix3.CreateTranslation(pos);
            var rotMat = Matrix3.CreateRotation((float)rot);

            Matrix3.Multiply(ref rotMat, ref posMat, out var transMat);

            return transMat;
        }

        protected virtual Matrix3 GetWorldMatrixInv()
        {
            IGameTiming timing = IoCManager.Resolve<IGameTiming>();
            if (timing.InSimulation)
                return _invWorldMatrix;

            // there really is no point trying to cache this because it will only be used in one frame
            var pos = GetLocalPosition();
            var rot = GetLocalRotation().Theta;

            var posMat = Matrix3.CreateTranslation(pos);
            var rotMat = Matrix3.CreateRotation((float)rot);
            var posImat = Matrix3.Invert(posMat);
            var rotImap = Matrix3.Invert(rotMat);

            Matrix3.Multiply(ref posImat, ref rotImap, out var itransMat);

            return itransMat;
        }

        private void RebuildMatrices()
        {
            var pos = _localPosition;
            var rot = _localRotation.Theta;

            var posMat = Matrix3.CreateTranslation(pos);
            var rotMat = Matrix3.CreateRotation((float)rot);

            Matrix3.Multiply(ref rotMat, ref posMat, out var transMat);

            _worldMatrix = transMat;

            var posImat = Matrix3.Invert(posMat);
            var rotImap = Matrix3.Invert(rotMat);

            Matrix3.Multiply(ref posImat, ref rotImap, out var itransMat);

            _invWorldMatrix = itransMat;
        }

        /// <summary>
        ///     Calculate our LocalCoordinates as if the location relative to our parent is equal to <paramref name="localPosition" />.
        /// </summary>
        private GridCoordinates LocalCoordinatesFor(Vector2 localPosition, GridId gridId)
        {
            if (Parent != null)
            {
                // transform localPosition from parent coords to world coords
                var worldPos = Parent.WorldMatrix.Transform(localPosition);
                var grid = IoCManager.Resolve<IMapManager>().GetGrid(gridId);
                var lc = new GridCoordinates(worldPos, grid.MapID);

                // then to parent grid coords
                return lc.ConvertToGrid(Parent.GridPosition.Grid);
            }
            else
            {
                return new GridCoordinates(localPosition, gridId);
            }
        }

        private void _recurseSetGridId(GridId gridId)
        {
            _gridID = gridId;
            foreach (var child in Children)
            {
                var cast = (TransformComponent) child;
                cast._recurseSetGridId(gridId);
            }
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

            public readonly GridId GridID;

            /// <summary>
            ///     Current rotation offset of the entity.
            /// </summary>
            public readonly Angle Rotation;

            /// <summary>
            ///     Constructs a new state snapshot of a TransformComponent.
            /// </summary>
            /// <param name="localPosition">Current position offset of this entity.</param>
            /// <param name="gridId">Current grid ID of this entity.</param>
            /// <param name="rotation">Current direction offset of this entity.</param>
            /// <param name="parentId">Current parent transform of this entity.</param>
            public TransformComponentState(Vector2 localPosition, GridId gridId, Angle rotation, EntityUid? parentId)
                : base(NetIDs.TRANSFORM)
            {
                LocalPosition = localPosition;
                GridID = gridId;
                Rotation = rotation;
                ParentID = parentId;
            }
        }
    }
}
