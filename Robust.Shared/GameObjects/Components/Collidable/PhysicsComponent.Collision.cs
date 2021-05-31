using System;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Called once when a collision starts
    /// </summary>
    public interface IStartCollide
    {
        /// <summary>
        ///     We'll pass in both our body and the other body to save the behaviors having to get these components themselves.
        /// </summary>
        [Obsolete("Use StartCollideEvent instead")]
        void CollideWith(Fixture ourFixture, Fixture otherFixture, in Manifold manifold);
    }

    /// <summary>
    ///     Called once when a collision ends.
    /// </summary>
    public interface IEndCollide
    {
        /// <summary>
        ///     Run behaviour after all other collision behaviors have run.
        /// </summary>
        [Obsolete("Use EndCollideEvent instead")]
        void CollideWith(Fixture ourFixture, Fixture otherFixture, in Manifold manifold);
    }

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
