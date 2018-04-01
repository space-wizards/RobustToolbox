using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Utility;
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
using SS14.Shared.Interfaces.GameObjects;
using SS14.Client.Interfaces;
using SS14.Client.Graphics.ClientEye;

namespace SS14.Client.GameObjects
{
    /// <summary>
    ///     Stores the position and orientation of the entity.
    /// </summary>
    public class ClientTransformComponent : Component, ITransformComponent
    {
        private Vector2 _position;
        public MapId MapID { get; private set; }
        public GridId GridID { get; private set; }
        public Angle LocalRotation { get; private set; }
        public virtual ITransformComponent Parent { get; private set; }

        private Matrix3 _worldMatrix;
        private Matrix3 _invWorldMatrix;


        public Matrix3 WorldMatrix
        {
            get
            {
                RebuildMatrices();
                if (Parent != null)
                {
                    var matP = Parent.WorldMatrix;
                    Matrix3.Multiply(ref matP, ref _worldMatrix, out var result);
                    return result;
                }
                return _worldMatrix;
            }
        }

        public Matrix3 InvWorldMatrix
        {
            get
            {
                RebuildMatrices();
                if (Parent != null)
                {
                    var matP = Parent.InvWorldMatrix;
                    Matrix3.Multiply(ref matP, ref _invWorldMatrix, out var result);
                    return result;
                }
                return _invWorldMatrix;
            }
        }

        /// <inheritdoc />
        public override string Name => "Transform";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.TRANSFORM;

        /// <inheritdoc />
        public override Type StateType => typeof(TransformComponentState);

        /// <inheritdoc />
        public event EventHandler<MoveEventArgs> OnMove;

        /// <inheritdoc />
        public event Action<Angle> OnRotate;

        //=> new LocalCoordinates(_position, GridID, MapID);
        public LocalCoordinates LocalPosition => LocalCoordinatesFor(_position, MapID, GridID);

        public Vector2 WorldPosition
        {
            get
            {
                if (Parent != null)
                {
                    return MatMult(Parent.WorldMatrix, _position);
                }

                var maps = IoCManager.Resolve<IMapManager>();
                if (maps.TryGetMap(MapID, out var map) && map.GridExists(GridID))
                {
                    return map.GetGrid(GridID).ConvertToWorld(_position);
                }
                return _position;
            }
        }

        public Angle WorldRotation
        {
            get
            {
                if (Parent != null)
                {
                    var wpRotV = Parent.WorldRotation.Theta;
                    var wRotV = wpRotV + LocalRotation.Theta;
                    return new Angle(wRotV);
                }
                return LocalRotation;
            }
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (TransformComponentState)state;
            if (LocalRotation != newState.Rotation)
            {
                LocalRotation = newState.Rotation;
                OnRotate?.Invoke(newState.Rotation);
            }

            if (_position != newState.LocalPosition || MapID != newState.MapID || GridID != newState.GridID)
            {
                var oldPos = LocalPosition;
                // TODO: this is horribly broken if the parent changes too, because the coordinates are all messed up.
                // Help.
                OnMove?.Invoke(this, new MoveEventArgs(oldPos, LocalCoordinatesFor(newState.LocalPosition, newState.MapID, newState.GridID)));
                SetPosition(newState.LocalPosition);
                MapID = newState.MapID;
                GridID = newState.GridID;
            }

            var newParentId = newState.ParentID;
            if (Parent?.Owner?.Uid != newParentId)
            {
                DetachParent();

                if (!newParentId.HasValue || !newParentId.Value.IsValid())
                    return;

                var newParent = Owner.EntityManager.GetEntity(newParentId.Value);
                AttachParent(newParent.GetComponent<ITransformComponent>());
            }
        }

        /// <summary>
        /// Detaches this entity from its parent.
        /// </summary>
        protected virtual void DetachParent()
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
        protected virtual void AttachParent(ITransformComponent parent)
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
            var pos = _position;
            var rot = LocalRotation.Theta;

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

        /// <summary>
        ///     Calculate our LocalCoordinates as if the location relative to our parent is equal to <paramref name="localPosition" />.
        /// </summary>
        private LocalCoordinates LocalCoordinatesFor(Vector2 localPosition, MapId mapId, GridId gridId)
        {
            if (Parent != null)
            {
                // transform localPosition from parent coords to world coords
                var worldPos = MatMult(Parent.WorldMatrix, localPosition);
                var lc = new LocalCoordinates(worldPos, GridId.DefaultGrid, mapId);

                // then to parent grid coords
                return lc.ConvertToGrid(Parent.LocalPosition.Grid);
            }
            else
            {
                return new LocalCoordinates(localPosition, gridId, mapId);
            }
        }

        // Hooks for GodotTransformComponent go here.
        protected virtual void SetPosition(Vector2 position)
        {
            _position = position;
        }
    }
}
