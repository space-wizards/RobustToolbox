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
    public class TransformComponent : Component, IClientTransformComponent
    {
        public Godot.Node2D SceneNode { get; private set; }

        private Vector2 _position;
        public MapId MapID { get; private set; }
        public GridId GridID { get; private set; }
        public Angle LocalRotation { get; private set; }
        public ITransformComponent Parent { get; private set; }

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

        public LocalCoordinates LocalPosition => new LocalCoordinates(_position, GridID, MapID);

        public Vector2 WorldPosition
        {
            get
            {
                if (Parent != null)
                {
                    return _position;
                }

                var maps = IoCManager.Resolve<IMapManager>();
                if (maps.TryGetMap(MapID, out var map) && map.GridExists(GridID))
                {
                    return map.GetGrid(GridID).ConvertToWorld(_position);
                }
                return new Vector2();
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

            if (_position != newState.Position || MapID != newState.MapID || GridID != newState.GridID)
            {
                OnMove?.Invoke(this, new MoveEventArgs(LocalPosition, new LocalCoordinates(newState.Position, newState.GridID, newState.MapID)));
                _position = newState.Position;
                SceneNode.Position = (_position * EyeManager.PIXELSPERMETER).Rounded().Convert();
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

        public override void OnAdd()
        {
            base.OnAdd();
            var holder = IoCManager.Resolve<ISceneTreeHolder>();
            SceneNode = new Godot.Node2D();
            SceneNode.SetName($"Transform {Owner.Uid} ({Owner.Name})");
            holder.WorldRoot.AddChild(SceneNode);
        }

        public override void OnRemove()
        {
            base.OnRemove();

            SceneNode.QueueFree();
            SceneNode.Dispose();
            SceneNode = null;
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
    }
}
