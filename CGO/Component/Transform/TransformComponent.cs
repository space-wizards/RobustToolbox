using System;
using GorgonLibrary;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    public class TransformComponent : GameObjectComponent
    {
        private Vector2D _position = Vector2D.Zero;

        public Vector2D Position
        {
            get { return _position; }
            set
            {
                _position = value;

                if (OnMove != null) OnMove(this, new VectorEventArgs(_position));
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
    }
}
