using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameObject;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Velocity;

namespace SGO
{
    public class VelocityComponent : Component
    {
        private Vector2 _velocity = Vector2.Zero;

        public Vector2 Velocity
        {
            get { return _velocity; }
            set { _velocity = value; }
        }

        
        public VelocityComponent() :base()
        {
            Family = ComponentFamily.Velocity;
        }

        public float X
        {
            get { return Velocity.X; }
            set { Velocity = new Vector2(value, Velocity.Y); }
        }

        public float Y
        {
            get { return Velocity.Y; }
            set { Velocity = new Vector2(Velocity.X, value);}
        }

        public override void Shutdown()
        {
            Velocity = Vector2.Zero;
        }

        public override ComponentState GetComponentState()
        {
            return new VelocityComponentState(Velocity.X, Velocity.Y);
        }
    }
}
