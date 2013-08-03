using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameObject;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Transform;
using ServerInterfaces.GOC;

namespace SGO
{
    class TransformComponent : Component, ITransformComponent
    {
        private Vector2 _position = Vector2.Zero;

        public Vector2 Position
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

        public float X
        {
            get { return Position.X; }
            set { Position = new Vector2(value, Position.Y); }
        }

        public float Y
        {
            get { return Position.Y; }
            set { Position = new Vector2(Position.X, value);}
        }

        public override void Shutdown()
        {
            Position = Vector2.Zero;
        }

        public void TranslateTo(Vector2 toPosition)
        {
            Position = toPosition;
        }

        public void TranslateByOffset(Vector2 offset)
        {
            Position = Position + offset;
        }

        public override ComponentState GetComponentState()
        {
            return new TransformComponentState(Position.X, Position.Y, false);
        }
    }
}
