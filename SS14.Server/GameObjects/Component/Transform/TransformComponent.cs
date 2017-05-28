using SFML.System;
using SS14.Server.Interfaces.GOC;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Transform;
using SS14.Shared.IoC;
using System;

namespace SS14.Server.GameObjects
{
    [IoCTarget]
    public class TransformComponent : Component, ITransformComponent
    {
        public override string Name => "Transform";
        private Vector2f _position = new Vector2f();
        private bool firstState = true;
        public TransformComponent()
        {
            Family = ComponentFamily.Transform;
        }

        public float X
        {
            get { return Position.X; }
            set { Position = new Vector2f(value, Position.Y); }
        }

        public float Y
        {
            get { return Position.Y; }
            set { Position = new Vector2f(Position.X, value); }
        }

        #region ITransformComponent Members

        public Vector2f Position
        {
            get { return _position; }
            set
            {
                Vector2f oldPosition = _position;
                _position = value;

                if (OnMove != null) OnMove(this, new VectorEventArgs(oldPosition, _position));
            }
        }

        public void TranslateTo(Vector2f toPosition)
        {
            Position = toPosition;
        }

        public void TranslateByOffset(Vector2f offset)
        {
            Position = Position + offset;
        }

        #endregion

        public event EventHandler<VectorEventArgs> OnMove;

        public override void Shutdown()
        {
            Position = new Vector2f();
        }

        public override ComponentState GetComponentState()
        {
            var state = new TransformComponentState(Position.X, Position.Y, firstState);
            firstState = false;
            return state;
        }
    }
}
