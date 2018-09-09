using System;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects.EntitySystemMessages;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
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
        private Vector2 _position; // holds offset from grid, or offset from parent
        private Angle _rotation; // local rotation
        private GridId _gridID;

        private Matrix3 _worldMatrix;
        private Matrix3 _invWorldMatrix;

        /// <inheritdoc />
        public event EventHandler<MoveEventArgs> OnMove;

        public event Action<ParentChangedEventArgs> OnParentChanged;

        /// <inheritdoc />
        public event Action<Angle> OnRotate;

        /// <inheritdoc />
        public sealed override string Name => "Transform";
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
            get => _gridID;
            private set => _gridID = value;
        }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public Angle LocalRotation
        {
            get => _rotation;
            set
            {
                SetRotation(value);
                RebuildMatrices();
                OnRotate?.Invoke(value);
                Dirty();
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        public Angle WorldRotation
        {
            get
            {
                if (_parent.IsValid())
                {
                    return Parent.WorldRotation + LocalRotation;
                }
                return _rotation;
            }
        }


        /// <summary>
        ///     Current parent entity of this entity.
        /// </summary>
        [ViewVariables]
        public ITransformComponent Parent
        {
            get => !_parent.IsValid() ? null : Owner.EntityManager.GetEntity(_parent).GetComponent<ITransformComponent>();
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
                    Matrix3.Multiply(ref _worldMatrix, ref parentMatrix, out var result);
                    return result;
                }
                return _worldMatrix;
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
                    Matrix3.Multiply(ref matP, ref _invWorldMatrix, out var result);
                    return result;
                }
                return _invWorldMatrix;
            }
        }

        public bool IsMapTransform => Parent == null;


        public virtual bool VisibleWhileParented { set; get; }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public GridLocalCoordinates LocalPosition
        {
            get
            {
                if (Parent != null)
                {
                    // transform _position from parent coords to world coords
                    var worldPos = MatMult(Parent.WorldMatrix, _position);
                    var lc = new GridLocalCoordinates(worldPos, MapID);

                    // then to parent grid coords
                    return lc.ConvertToGrid(Parent.LocalPosition.Grid);
                }
                else
                {
                    return new GridLocalCoordinates(_position, _gridID);
                }
            }
            set
            {
                if (_parent.IsValid())
                {
                    // grid coords to world coords
                    var worldCoords = value.ToWorld();

                    // world coords to parent coords
                    var newPos = MatMult(Parent.InvWorldMatrix, worldCoords.Position);

                    // float rounding error guard, if the offset is less than 1mm ignore it
                    if (Math.Abs(newPos.LengthSquared - _position.LengthSquared) < 10.0E-3)
                        return;

                    SetPosition(newPos);
                }
                else
                {
                    SetPosition(value.Position);

                    GridID = value.GridID;
                }

                Dirty();

                RebuildMatrices();
                OnMove?.Invoke(this, new MoveEventArgs(LocalPosition, value));
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
                    return MatMult(Parent.WorldMatrix, _position);
                }
                else
                {
                    // Work around a client-side race condition of the grids not being synced yet.
                    // Maybe it's better to fix the race condition instead.
                    // Eh.
                    if (IoCManager.Resolve<IMapManager>().TryGetGrid(GridID, out var grid))
                    {
                        return grid.ConvertToWorld(_position);
                    }
                    return _position;
                }
            }
            set
            {
                if (_parent.IsValid())
                {
                    // world coords to parent coords
                    var newPos = MatMult(Parent.InvWorldMatrix, value);

                    // float rounding error guard, if the offset is less than 1mm ignore it
                    if (Math.Abs(newPos.LengthSquared - _position.LengthSquared) < 10.0E-3)
                        return;

                    SetPosition(newPos);
                }
                else
                {
                    SetPosition(value);
                    GridID = IoCManager.Resolve<IMapManager>().GetMap(MapID).FindGridAt(_position).Index;
                }

                Dirty();

                RebuildMatrices();
                OnMove?.Invoke(this, new MoveEventArgs(LocalPosition, new GridLocalCoordinates(_position, GridID)));
            }
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
            var worldPos = MatMult(Parent.WorldMatrix, _position);
            var lc = new GridLocalCoordinates(worldPos, MapID);

            // then to parent grid coords
            lc = lc.ConvertToGrid(Parent.LocalPosition.Grid);

            // detach
            Parent = null;

            // switch position back to grid coords
            LocalPosition = lc;

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

            Parent = parent;

            // move to parents grid
            _gridID = parent.GridID;

            // offset position from world to parent
            SetPosition(MatMult(parent.InvWorldMatrix, _position));
            RebuildMatrices();
            Dirty();
        }

        public void AttachParent(IEntity parent)
        {
            var transform = parent.GetComponent<ITransformComponent>();
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
                return ContainsEntity(entityTransform.Parent); //Recursively search up the entitys containers for this object
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _parent, "parent", new EntityUid());
            serializer.DataField(ref _gridID, "grid", GridId.Nullspace);
            serializer.DataField(ref _position, "pos", Vector2.Zero);
            serializer.DataField(ref _rotation, "rot", new Angle());
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new TransformComponentState(_position, GridID, LocalRotation, Parent?.Owner?.Uid);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (TransformComponentState)state;

            var newParentId = newState.ParentID;
            if (Parent?.Owner?.Uid != newParentId)
            {
                DetachParent();

                if (newParentId.HasValue && newParentId.Value.IsValid())
                {
                    var newParent = Owner.EntityManager.GetEntity(newParentId.Value);
                    AttachParent(newParent.GetComponent<ITransformComponent>());
                }
            }

            var rebuildMatrices = false;
            if (LocalRotation != newState.Rotation)
            {
                SetRotation(newState.Rotation);
                OnRotate?.Invoke(newState.Rotation);
                rebuildMatrices = true;
            }

            if (_position != newState.LocalPosition || GridID != newState.GridID)
            {
                var oldPos = LocalPosition;
                SetPosition(newState.LocalPosition);
                GridID = newState.GridID;
                // TODO: this is horribly broken if the parent changes too, because the coordinates are all messed up.
                // Help.
                OnMove?.Invoke(this, new MoveEventArgs(oldPos, LocalPosition));
                rebuildMatrices = true;
            }

            if (rebuildMatrices)
            {
                RebuildMatrices();
            }
        }

        // Hooks for GodotTransformComponent go here.
        protected virtual void SetPosition(Vector2 position)
        {
            _position = position;
        }

        protected virtual void SetRotation(Angle rotation)
        {
            _rotation = rotation;
        }

        private static Vector2 MatMult(Matrix3 mat, Vector2 vec)
        {
            var vecHom = new Vector3(vec.X, vec.Y, 1);
            Matrix3.Transform(ref mat, ref vecHom);
            return vecHom.Xy;
        }

        private void RebuildMatrices()
        {
            var pos = _position;
            var rot = _rotation.Theta;

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
        private GridLocalCoordinates LocalCoordinatesFor(Vector2 localPosition, GridId gridId)
        {
            if (Parent != null)
            {
                // transform localPosition from parent coords to world coords
                var worldPos = MatMult(Parent.WorldMatrix, localPosition);
                var grid = IoCManager.Resolve<IMapManager>().GetGrid(gridId);
                var lc = new GridLocalCoordinates(worldPos, grid.MapID);

                // then to parent grid coords
                return lc.ConvertToGrid(Parent.LocalPosition.Grid);
            }
            else
            {
                return new GridLocalCoordinates(localPosition, gridId);
            }
        }

        public string GetDebugString()
        {
            return $"pos/rot/wpos/wrot: {LocalPosition}/{LocalRotation}/{WorldPosition}/{WorldRotation}";
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
            /// <param name="position">Current position offset of the entity.</param>
            /// <param name="rotation">Current direction offset of the entity.</param>
            /// <param name="parentID">Current parent transform of this entity.</param>
            public TransformComponentState(Vector2 localPosition, GridId gridId, Angle rotation, EntityUid? parentID)
                : base(NetIDs.TRANSFORM)
            {
                LocalPosition = localPosition;
                GridID = gridId;
                Rotation = rotation;
                ParentID = parentID;
            }
        }
    }
}
