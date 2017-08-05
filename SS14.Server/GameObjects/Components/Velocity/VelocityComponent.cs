using SFML.System;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;

namespace SS14.Server.GameObjects
{
    public class VelocityComponent : Component, IVelocityComponent
    {
        public override string Name => "Velocity";
        public override uint? NetID => NetIDs.VELOCITY;

        public Vector2f Velocity { get; set; }

        public float X
        {
            get => Velocity.X;
            set => Velocity = new Vector2f(value, Velocity.Y);
        }

        public float Y
        {
            get => Velocity.Y;
            set => Velocity = new Vector2f(Velocity.X, value);
        }

        public override void Shutdown()
        {
            Velocity = new Vector2f();
        }

        public override ComponentState GetComponentState()
        {
            return new VelocityComponentState(Velocity.X, Velocity.Y);
        }
    }
}
