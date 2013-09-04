using System;
using GameObject;
using GorgonLibrary;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Transform;

namespace CGO
{
    public class TransformComponent : Component
    {
        private Vector2D _position = Vector2D.Zero;
        private TransformComponentState lastState;
        private TransformComponentState previousState;

        public TransformComponent()
        {
            Family = ComponentFamily.Transform;
        }

        public Vector2D Position
        {
            get { return _position; }
            set
            {
                Vector2D oldPosition = _position;
                _position = value;

                if (OnMove != null) OnMove(this, new VectorEventArgs(Vector2TypeConverter.ToVector2(oldPosition), Vector2TypeConverter.ToVector2(_position)));
            }
        }

        public override Type StateType
        {
            get { return typeof (TransformComponentState); }
        }

        public float X
        {
            get { return Position.X; }
            set { Position = new Vector2D(value, Position.Y); }
        }

        public float Y
        {
            get { return Position.Y; }
            set { Position = new Vector2D(Position.X, value); }
        }

        public event EventHandler<VectorEventArgs> OnMove;

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
            var diff = (toVector - Position).Length;
            if (diff > 0.1f && 
                (
                    state.ForceUpdate
                 || Owner.GetComponent<KeyBindingInputComponent>(ComponentFamily.Input) == null
                 || diff > 60.0f)
                )
                TranslateTo(toVector);
        }
    }
}