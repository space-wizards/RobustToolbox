using SFML.System;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components;
using SS14.Shared.GameObjects.Components.Transform;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using System;

namespace SS14.Server.GameObjects
{
    public class TransformComponent : Component, ITransformComponent
    {
        public override string Name => "Transform";
        public override uint? NetID => NetIDs.TRANSFORM;
        private Vector2f _position = new Vector2f();
        private bool firstState = true;

        #region ITransformComponent Members

        public event EventHandler<VectorEventArgs> OnMove;

        public float X
        {
            get => Position.X;
            set => Position = new Vector2f(value, Position.Y);
        }

        public float Y
        {
            get => Position.Y;
            set => Position = new Vector2f(Position.X, value);
        }

        public Vector2f Position
        {
            get => _position;
            set
            {
                Vector2f oldPosition = _position;
                _position = value;

                OnMove?.Invoke(this, new VectorEventArgs(oldPosition, _position));
            }
        }

        public void Offset(Vector2f offset)
        {
            Position += offset;
        }

        #endregion

        public override void Shutdown()
        {
            Position = new Vector2f(0, 0);
        }

        public override ComponentState GetComponentState()
        {
            var state = new TransformComponentState(Position, firstState);
            firstState = false;
            return state;
        }
    }
}
