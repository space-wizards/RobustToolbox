using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using System;
using SS14.Shared.Enums;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.GameObjects
{
    /// <summary>
    ///     Stores the position and orientation of the entity.
    /// </summary>
    public class TransformComponent : Component, ITransformComponent
    {
        private Vector2 _position;
        public MapId MapID { get; private set; }
        public GridId GridID { get; private set; }
        public Angle Rotation { get; private set; }
        public ITransformComponent Parent { get; private set; }

        private Matrix3 _worldMatrix;
        private Matrix3 _invWorldMatrix;

        public Matrix3 WorldMatrix => _worldMatrix;
        public Matrix3 InvWorldMatrix => _invWorldMatrix;

        /// <inheritdoc />
        public override string Name => "Transform";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.TRANSFORM;

        /// <inheritdoc />
        public override Type StateType => typeof(TransformComponentState);

        /// <inheritdoc />
        public event EventHandler<MoveEventArgs> OnMove;

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
                    return new LocalCoordinates(_position, GridID, MapID);
                }
            }
        }

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
                    var maps = IoCManager.Resolve<IMapManager>();
                    if (maps.TryGetMap(MapID, out var map) && map.GridExists(GridID))
                    {
                        return map.GetGrid(GridID).ConvertToWorld(_position);
                    }
                    return new Vector2();
                }
            }
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (TransformComponentState)state;
            Rotation = newState.Rotation;
            RebuildMatrices();

            if (_position != newState.Position || MapID != newState.MapID || GridID != newState.GridID)
            {
                OnMove?.Invoke(this, new MoveEventArgs(LocalPosition, new LocalCoordinates(newState.Position, newState.GridID, newState.MapID)));
                _position = newState.Position;
                MapID = newState.MapID;
                GridID = newState.GridID;
            }

            var newParentId = newState.ParentID;
            if (Parent?.Owner?.Uid != newParentId)
            {
                DetachParent();

                if(!newParentId.HasValue || !newParentId.Value.IsValid())
                    return;
                
                var newParent = Owner.EntityManager.GetEntity(newParentId.Value);
                AttachParent(newParent.GetComponent<ITransformComponent>());
            }
        }

        /// <summary>
        /// Detaches this entity from its parent.
        /// </summary>
        private void DetachParent()
        {
            // nothing to do
            if (Parent == null)
                return;

            Parent = null;
        }

        /// <summary>
        /// Sets another entity as the parent entity.
        /// </summary>
        /// <param name="parent"></param>
        private void AttachParent(ITransformComponent parent)
        {
            // nothing to attach to.
            if (parent == null)
                return;

            Parent = parent;
        }

        public ITransformComponent GetMapTransform()
        {
            if (Parent != null) //If we are not the final transform, query up the chain of parents
            {
                return Parent.GetMapTransform();
            }
            return this;
        }

        public bool IsMapTransform => Parent == null;

        /// <summary>
        ///     Does this entity contain the entity in the argument
        /// </summary>
        public bool ContainsEntity(ITransformComponent transform)
        {
            if (transform.IsMapTransform) //Is the entity on the map
            {
                if (this == transform.Parent) //Is this the direct container of the entity
                {
                    return true;
                }
                else
                {
                    return ContainsEntity(transform.Parent); //Recursively search up the entitys containers for this object
                }
            }
            return false;
        }

        private void RebuildMatrices()
        {
            var pos = WorldPosition;
            var rot = Rotation.Theta;

            var posMat = Matrix3.CreateTranslation(pos);
            var rotMat = Matrix3.CreateRotation((float)rot);

            Matrix3.Multiply(ref rotMat, ref posMat, out var transMat);

            _worldMatrix = transMat;
            _invWorldMatrix = Matrix3.Invert(transMat);
        }

        private static Vector2 MatMult(Matrix3 mat, Vector2 vec)
        {
            var vecHom = new Vector3(vec.X, vec.Y, 1);
            Matrix3.Transform(ref mat, ref vecHom);
            return vecHom.Xy;
        }
    }
}
