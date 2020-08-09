using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///
    /// </summary>
    public interface IPhysBody
    {
        /// <summary>
        ///     Entity that this physBody represents.
        /// </summary>
        IEntity Entity { get; }

        /// <summary>
        ///     AABB of this entity in world space.
        /// </summary>
        Box2 WorldAABB { get; }

        /// <summary>
        ///     AABB of this entity in local space.
        /// </summary>
        Box2 AABB { get; }

        IList<IPhysShape> PhysicsShapes { get; }

        /// <summary>
        /// Whether or not this body can collide.
        /// </summary>
        bool CanCollide { get; set; }

        /// <summary>
        /// Bitmask of the collision layers this body is a part of. The layers are calculated from
        /// all of the shapes of this body.
        /// </summary>
        int CollisionLayer { get; }

        /// <summary>
        /// Bitmask of the layers this body collides with. The mask is calculated from
        /// all of the shapes of this body.
        /// </summary>
        int CollisionMask { get; }

        /// <summary>
        ///     The map index this physBody is located upon
        /// </summary>
        MapId MapID { get; }

        /// <summary>
        /// Broad Phase proxy ID.
        /// </summary>
        int ProxyId { get; set; }

        /// <summary>
        /// The type of the body, which determines how collisions effect this object.
        /// </summary>
        BodyType BodyType { get; set; }

        int SleepAccumulator { get; set; }

        int SleepThreshold { get; set; }

        bool Awake { get; }

        /// <summary>
        ///     Non-hard collidables will not cause action collision (e.g. blocking of movement)
        ///     while still raising collision events.
        /// </summary>
        /// <remarks>
        ///     This is useful for triggers or such to detect collision without actually causing a blockage.
        /// </remarks>
        bool Hard { get; set; }

        /// <summary>
        /// Inverse mass of the entity in kilograms (1 / Mass).
        /// </summary>
        float InvMass { get; }

        /// <summary>
        /// Inverse moment of inertia, in 
        /// </summary>
        float InvI { get; }

        /// <summary>
        /// Current Force being applied to this entity in Newtons.
        /// </summary>
        /// <remarks>
        /// The force is applied to the center of mass.
        /// https://en.wikipedia.org/wiki/Force
        /// </remarks>
        Vector2 Force { get; set; }

        /// <summary>
        /// Current torque being applied to this entity in N*m.
        /// </summary>
        /// <remarks>
        /// The torque rotates around the Z axis on the object.
        /// https://en.wikipedia.org/wiki/Torque
        /// </remarks>
        float Torque { get; set; }

        /// <summary>
        /// Sliding friction coefficient. This is how slippery a material is,
        /// or how much of it's velocity is being countered.
        /// </summary>
        /// <remarks>
        /// This value ranges from 0 to greater than one.
        /// Ice is 0.03, steel is 0.4, rubber is 1.
        /// </remarks>
        float Friction { get; set; }

        void WakeBody();

        /// <summary>
        /// Derived value determining if this body can move or not.
        /// </summary>
        /// <returns>True if this body can move, false if it is static.</returns>
        bool CanMove();
    }
}
