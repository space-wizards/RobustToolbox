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
        public Angle Rotation { get; private set; }
        public ITransformComponent Parent { get; private set; }
        //TODO: Make parenting actually work.

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
                    return GetMapTransform().WorldPosition; //Search up the tree for the true map position
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
            if (Rotation != newState.Rotation)
            {
                Rotation = newState.Rotation;
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

            if (Parent?.Owner?.Uid != newState.ParentID)
            {
                DetachParent();
                if (!(newState.ParentID is EntityUid parentID))
                {
                    return;
                }
                var newParent = Owner.EntityManager.GetEntity(parentID);
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

        public override void OnAdd(IEntity owner)
        {
            base.OnAdd(owner);
            var holder = IoCManager.Resolve<ISceneTreeHolder>();
            SceneNode = new Godot.Node2D();
            SceneNode.SetName($"Transform {owner.Uid} ({owner.Name})");
            holder.WorldRoot.AddChild(SceneNode);
        }

        public override void OnRemove()
        {
            base.OnRemove();

            SceneNode.QueueFree();
            SceneNode.Dispose();
            SceneNode = null;
        }
    }
}
