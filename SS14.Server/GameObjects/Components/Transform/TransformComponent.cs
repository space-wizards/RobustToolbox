using System;
using OpenTK;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;

namespace SS14.Server.GameObjects
{
    /// <summary>
    ///     Stores the position and orientation of the entity.
    /// </summary>
    public class TransformComponent : Component, ITransformComponent
    {
        private Vector2 _position;

        public Vector2 Rotation { get; set; }

        public override string Name => "Transform";
        public override uint? NetID => NetIDs.TRANSFORM;

        public event EventHandler<VectorEventArgs> OnMove;

        /// <inheritdoc />
        public Vector2 Position
        {
            get => _position;
            set
            {
                var oldPosition = _position;
                _position = value;

                OnMove?.Invoke(this, new VectorEventArgs(oldPosition, _position));
            }
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new TransformComponentState(Position, Rotation);
        }
    }
}
