using System;
using System.Collections.Generic;
using OpenTK;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Maths;

namespace SS14.Client.GameObjects
{
    /// <summary>
    ///     Stores the position and orientation of the entity.
    /// </summary>
    public class TransformComponent : Component, ITransformComponent
    {
        public Vector2 Position { get; private set; }
        public Angle Rotation { get; private set; }
        public ITransformComponent Parent { get; set; }

        //TODO: Make parenting actually work.

        /// <inheritdoc />
        public override string Name => "Transform";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.TRANSFORM;

        /// <inheritdoc />
        public override Type StateType => typeof(TransformComponentState);

        /// <inheritdoc />
        public event EventHandler<VectorEventArgs> OnMove;

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (TransformComponentState)state;
            Rotation = newState.Rotation;

            if (Position != newState.Position)
            {
                OnMove?.Invoke(this, new VectorEventArgs(Position, newState.Position));
                Position = newState.Position;
            }

            if (Parent != newState.Parent)
            {
                DetachParent();
                AttachParent(newState.Parent);
            }
        }

        /// <summary>
        /// Detaches this entity from its parent.
        /// </summary>
        public void DetachParent()
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
        public void AttachParent(ITransformComponent parent)
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

        /// <summary>
        ///     Does this entity contain the entity in the argument
        /// </summary>
        public bool ContainsEntity(ITransformComponent entity)
        {
            if(entity.Parent != null) //Is the entity on the map
                if(this == entity.Parent) //Is this the direct container of the entity
                {
                    return true;
                }
                else
                {
                    return ContainsEntity(entity.Parent); //Recursively search up the entitys containers for this object
                }
            return false;
        }
    }
}
