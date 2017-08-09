using System;
using OpenTK;
using SS14.Shared;
using SS14.Shared.GameObjects;
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

        public override string Name => "Transform";
        public override uint? NetID => NetIDs.TRANSFORM;

        public override Type StateType => typeof(TransformComponentState);

        public event EventHandler<VectorEventArgs> OnMove;

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (TransformComponentState) state;
            Rotation = newState.Rotation;

            if (Position == newState.Position)
                return;

            OnMove?.Invoke(this, new VectorEventArgs(Position, newState.Position));
            Position = newState.Position;
        }
    }
}
