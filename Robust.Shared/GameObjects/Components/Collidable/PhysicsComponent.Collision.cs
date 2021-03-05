using System;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Called every tick for colliding bodies. Called once per pair.
    /// </summary>
    public sealed class CollisionMessage : EntitySystemMessage
    {
        public readonly IPhysBody BodyA;
        public readonly IPhysBody BodyB;
        public readonly float FrameTime;
        public readonly Manifold Manifold;

        void RemovedFromPhysicsTree(MapId mapId);
        void AddedToPhysicsTree(MapId mapId);
    }

    public partial class PhysicsComponent : Component, IPhysicsComponent
    {
        public CollisionMessage(IPhysBody bodyA, IPhysBody bodyB, float frameTime, Manifold manifold)
        {
            BodyA = bodyA;
            BodyB = bodyB;
            FrameTime = frameTime;
            Manifold = manifold;
        }
    }

    /// <summary>
    ///     Called once when a collision starts
    /// </summary>
    public interface IStartCollide
    {
        /// <summary>
        ///     We'll pass in both our body and the other body to save the behaviors having to get these components themselves.
        /// </summary>
        void CollideWith(IPhysBody ourBody, IPhysBody otherBody, in Manifold manifold);
    }

    /// <summary>
    ///     Called once when a collision ends.
    /// </summary>
    public interface IEndCollide
    {
        /// <summary>
        ///     Run behaviour after all other collision behaviors have run.
        /// </summary>
        /// <param name="ourBody"></param>
        /// <param name="otherBody"></param>
        /// <param name="manifold"></param>
        void CollideWith(IPhysBody ourBody, IPhysBody otherBody, in Manifold manifold);
    }

    public interface ICollideSpecial
    {
        bool PreventCollide(IPhysBody collidedwith);
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
    public sealed class PhysicsUpdateMessage : EntitySystemMessage
    {
        public PhysicsComponent Component { get; }

        public PhysicsUpdateMessage(PhysicsComponent component)
        {
            Component = component;
        }
    }

    public sealed class FixtureUpdateMessage : EntitySystemMessage
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
