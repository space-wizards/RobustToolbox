using System;
using System.Collections.Generic;
using Robust.Shared.Containers;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Called every tick for colliding bodies.
    /// </summary>
    public interface ICollideBehavior
    {
        /// <summary>
        ///     We'll pass in both our body and the other body to save the behaviors having to get these components themselves.
        /// </summary>
        void CollideWith(IPhysBody ourBody, IPhysBody otherBody, in Manifold manifold);
    }

    public interface IPostCollide
    {
        /// <summary>
        ///     Run behaviour after all other collision behaviors have run.
        /// </summary>
        /// <param name="ourBody"></param>
        /// <param name="otherBody"></param>
        void PostCollide(IPhysBody ourBody, IPhysBody otherBody);
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
