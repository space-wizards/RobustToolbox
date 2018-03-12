using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using System;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects.Serialization;
using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Server.GameObjects
{
    /// <summary>
    ///     Stores the position and orientation of the entity.
    /// </summary>
    public class TransformComponent : Component, IServerTransformComponent
    {
        /// <summary>
        ///     Current parent entity of this entity.
        /// </summary>
        public IServerTransformComponent Parent
        {
            get => !_parent.IsValid() ? null : IoCManager.Resolve<IServerEntityManager>().GetEntity(_parent).GetComponent<IServerTransformComponent>();
            private set => _parent = value?.Owner.Uid ?? EntityUid.Invalid;
        }

        ITransformComponent ITransformComponent.Parent => Parent;

        private EntityUid _parent;
        private Vector2 _position; // holds offset from grid, or offset from parent
        private Angle _rotation; // local rotation
        private MapId _mapID;
        private GridId _gridID;

        private Matrix3 _worldMatrix;
        private Matrix3 _invWorldMatrix;

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

        /// <inheritdoc />
        public MapId MapID
        {
            get => _mapID;
            private set => _mapID = value;
        }

        /// <inheritdoc />
        public GridId GridID
        {
            get => _gridID;
            private set => _gridID = value;
        }

        /// <summary>
        ///     Current rotation offset of the entity.
        /// </summary>
        public Angle LocalRotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                RebuildMatrices();
                OnRotate?.Invoke(value);
            }
        }

        /// <inheritdoc />
        public Angle WorldRotation
        {
            get
            {
                if (_parent.IsValid())
                {
                    var lRotV = _rotation.ToVec();
                    var wRotV = MatMult(Parent.WorldMatrix, lRotV);
                    var uwRotV = wRotV.Normalized;
                    return uwRotV.ToAngle();
                }
                return _rotation;
            }
        }

        /// <inheritdoc />
        public override string Name => "Transform";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.TRANSFORM;

        /// <inheritdoc />
        public event EventHandler<MoveEventArgs> OnMove;

        /// <inheritdoc />
        public event Action<Angle> OnRotate;

        /// <inheritdoc />
        public LocalCoordinates LocalPosition
        {
            get
            {
                if (Parent != null)
                {
                    // transform _position from parent coords to world coords
                    var worldPos = MatMult(Parent.WorldMatrix, _position);
                    var lc = new LocalCoordinates(worldPos, GridId.DefaultGrid, MapID);

                    // then to parent grid coords
                    return lc.ConvertToGrid(Parent.LocalPosition.Grid);
                }
                else
                {
                    return new LocalCoordinates(_position, _gridID, _mapID);
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

                    _position = newPos;
                }
                else
                {
                    _position = value.Position;

                    MapID = value.MapID;
                    GridID = value.GridID;
                }

                RebuildMatrices();
                OnMove?.Invoke(this, new MoveEventArgs(LocalPosition, value));
            }
        }

        /// <inheritdoc />
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
                    return IoCManager.Resolve<IMapManager>().GetMap(MapID).GetGrid(GridID).ConvertToWorld(_position);
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

                    _position = newPos;
                }
                else
                {
                    _position = value;
                    GridID = IoCManager.Resolve<IMapManager>().GetMap(MapID).FindGridAt(_position).Index;
                }

                RebuildMatrices();
                OnMove?.Invoke(this, new MoveEventArgs(LocalPosition, new LocalCoordinates(_position, GridID, MapID)));
            }
        }

        public override void ExposeData(EntitySerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _parent, "parent", new EntityUid());
            serializer.DataField(ref _mapID, "map", MapId.Nullspace);
            serializer.DataField(ref _gridID, "grid", GridId.DefaultGrid);
            serializer.DataField(ref _position, "pos", Vector2.Zero);
            serializer.DataField(ref _rotation, "rot", new Angle());
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new TransformComponentState(LocalPosition, LocalRotation, Parent?.Owner?.Uid);
        }

        /// <summary>
        /// Detaches this entity from its parent.
        /// </summary>
        public void DetachParent()
        {
            // nothing to do
            if (Parent == null)
                return;

            // transform _position from parent coords to world coords
            var worldPos = MatMult(Parent.WorldMatrix, _position);
            var lc = new LocalCoordinates(worldPos, GridId.DefaultGrid, MapID);

            // then to parent grid coords
            lc = lc.ConvertToGrid(Parent.LocalPosition.Grid);

            // detach
            Parent = null;

            // switch position back to grid coords
            LocalPosition = lc;
        }

        /// <summary>
        /// Sets another entity as the parent entity.
        /// </summary>
        /// <param name="parent"></param>
        public void AttachParent(IServerTransformComponent parent)
        {
            // nothing to attach to.
            if (parent == null)
                return;

            Parent = parent;

            // move to parents grid
            _mapID = parent.MapID;
            _gridID = parent.GridID;

            // offset position from world to parent
            _position = MatMult(parent.InvWorldMatrix, _position);
            RebuildMatrices();
        }

        public void AttachParent(IEntity entity)
        {
            var transform = entity.GetComponent<IServerTransformComponent>();
            AttachParent(transform);
        }

        /// <summary>
        ///     Finds the transform of the entity located on the map itself
        /// </summary>
        public IServerTransformComponent GetMapTransform()
        {
            if (Parent != null) //If we are not the final transform, query up the chain of parents
            {
                return Parent.GetMapTransform();
            }
            return this;
        }

        ITransformComponent ITransformComponent.GetMapTransform() => GetMapTransform();

        public bool IsMapTransform => Parent == null;

        /// <summary>
        ///     Does this entity contain the entity in the argument
        /// </summary>
        public bool ContainsEntity(ITransformComponent transform)
        {
            if (transform.IsMapTransform) //Is the entity on the map
            {
                return false;
            }

            if (this == transform.Parent) //Is this the direct container of the entity
            {
                return true;
            }
            else
            {
                return ContainsEntity(transform.Parent); //Recursively search up the entitys containers for this object
            }
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

        private static Vector2 MatMult(Matrix3 mat, Vector2 vec)
        {
            var vecHom = new Vector3(vec.X, vec.Y, 1);
            Matrix3.Transform(ref mat, ref vecHom);
            return vecHom.Xy;
        }
    }
}
