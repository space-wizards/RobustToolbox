using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using System;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.GameObjects
{
    /// <summary>
    ///     Stores the position and orientation of the entity.
    /// </summary>
    public class TransformComponent : Component, ITransformComponent
    {
        private Vector2 _position;
        public int MapID { get; private set; }
        public int GridID { get; private set; }
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
                    return IoCManager.Resolve<IMapManager>().GetMap(MapID).GetGrid(GridID).ConvertToWorld(_position);
                }
            }
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (TransformComponentState)state;
            Rotation = newState.Rotation;

            if (_position != newState.Position || MapID != newState.MapID || GridID != newState.GridID)
            {
                OnMove?.Invoke(this, new MoveEventArgs(LocalPosition, new LocalCoordinates(newState.Position, newState.GridID, newState.MapID)));
                _position = newState.Position;
                MapID = newState.MapID;
                GridID = newState.GridID;
            }

            if (Parent?.Owner?.Uid != newState.ParentID)
            {
                DetachParent();
                if (!(newState.ParentID is int parentID))
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
    }
}
