using System;
using System.Collections.Generic;
using OpenTK;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;

namespace SS14.Client.GameObjects
{
    /// <summary>
    ///     Stores the position and orientation of the entity.
    /// </summary>
    public class TransformComponent : ClientComponent, ITransformComponent
    {
        public Vector2 Position { get; private set; }
        public Vector2 Rotation { get; private set; }
        public IEntity Parent { get; private set; }

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
            var newState = (TransformComponentState) state;
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
        private void DetachParent()
        {
            // nothing to do
            if(Parent == null)
                return;

            Parent = null;
        }

        /// <summary>
        /// Sets another entity as the parent entity.
        /// </summary>
        /// <param name="parent"></param>
        private void AttachParent(IEntity parent)
        {
            // nothing to attach to.
            if(parent == null)
                return;

            Parent = parent;
        }
    }
}
