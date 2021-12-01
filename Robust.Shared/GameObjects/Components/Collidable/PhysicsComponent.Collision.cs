using System;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public enum BodyStatus: byte
    {
        OnGround,
        InAir
    }

    /// <summary>
    ///     Sent whenever a <see cref="IPhysBody"/> is changed.
    /// </summary>
    public sealed class PhysicsUpdateMessage : EntityEventArgs
    {
        public PhysicsComponent Component { get; }

        public PhysicsUpdateMessage(PhysicsComponent component)
        {
            Component = component;
        }
    }

    public sealed class FixtureUpdateMessage : EntityEventArgs
    {
        public PhysicsComponent Body { get; }

        public Fixture Fixture { get; }

        public FixtureUpdateMessage(PhysicsComponent body, Fixture fixture)
        {
            Body = body;
            Fixture = fixture;
        }
    }
}
