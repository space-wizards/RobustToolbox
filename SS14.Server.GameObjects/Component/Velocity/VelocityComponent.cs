using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Velocity;
using SS14.Shared.Maths;

namespace SS14.Server.GameObjects
{
    public class VelocityComponent : Component
    {
        private Vector2 _velocity = Vector2.Zero;


        public VelocityComponent()
        {
            Family = ComponentFamily.Velocity;
        }

        public Vector2 Velocity
        {
            get { return _velocity; }
            set { _velocity = value; }
        }

        public float X
        {
            get { return Velocity.X; }
            set { Velocity = new Vector2(value, Velocity.Y); }
        }

        public float Y
        {
            get { return Velocity.Y; }
            set { Velocity = new Vector2(Velocity.X, value); }
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