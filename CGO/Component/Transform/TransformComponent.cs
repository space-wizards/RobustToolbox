using System;
using GorgonLibrary;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Transform;

namespace CGO
{
    public class TransformComponent : GameObjectComponent
    {
        private Vector2D _position = Vector2D.Zero;
        private TransformComponentState previousState;
        private TransformComponentState lastState;

        public Vector2D Position
        {
            get { return _position; }
            set
            {
                var oldPosition = _position;
                _position = value;

                if (OnMove != null) OnMove(this, new VectorEventArgs(oldPosition, _position));
            }
        }

        public event EventHandler<VectorEventArgs> OnMove;
        
        public TransformComponent() :base()
        {
            Family = ComponentFamily.Transform;
        }

        public override Type StateType
        {
            get { return typeof(TransformComponentState); }
        }

        public float X
        {
            get { return Position.X; }
            set { Position = new Vector2D(value, Position.Y); }
        }

        public float Y
        {
            get { return Position.Y; }
            set { Position = new Vector2D(Position.X, value);}
        }

        public override void Shutdown()
        {
            Position = Vector2D.Zero;
        }

        public void TranslateTo(Vector2D toPosition)
        {
            Position = toPosition;
        }

        public void TranslateByOffset(Vector2D offset)
        {
            Position = Position + offset;
        }

        public override void HandleComponentState(dynamic state)
        {
            SetNewState(state);
        }

        private void SetNewState(TransformComponentState state)
        {
            if (lastState != null)
                previousState = lastState;
            lastState = state;
            var toVector = new Vector2D(state.X, state.Y);
            if(state.ForceUpdate 
                || Owner.GetComponent<KeyBindingInputComponent>(ComponentFamily.Input) == null
                || (toVector - Position).Length > 100)
                TranslateTo(toVector);
        }
    }
}
