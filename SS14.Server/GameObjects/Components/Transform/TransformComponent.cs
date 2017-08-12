using System;
using OpenTK;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Maths;

namespace SS14.Server.GameObjects
{
    /// <summary>
    ///     Stores the position and orientation of the entity.
    /// </summary>
    public class TransformComponent : Component, ITransformComponent
    {
        /// <summary>
        ///     Current parent entity of this entity.
        /// </summary>
        public IEntity Parent { get; set; }

        private Vector2 _position;

        /// <summary>
        ///     Current rotation offset of the entity.
        /// </summary>
        public Angle Rotation { get; set; }

        /// <inheritdoc />
        public override string Name => "Transform";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.TRANSFORM;

        /// <inheritdoc />
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
            return new TransformComponentState(Position, Rotation, Parent);
        }
    }
}
