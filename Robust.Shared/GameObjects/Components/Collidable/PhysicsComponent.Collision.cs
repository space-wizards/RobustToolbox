using System;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects.Components
{
    public interface ICollideBehavior
    {
        void CollideWith(IEntity collidedWith);

        /// <summary>
        ///     Called after all collisions have been processed, as well as how many collisions occured
        /// </summary>
        /// <param name="collisionCount"></param>
        void PostCollide(uint collisionCount) { }
    }

    [Serializable, NetSerializable]
    public enum BodyStatus
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
}
