using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Contacts;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///
    /// </summary>
    public interface IPhysBody : IComponent
    {
        bool IgnoreGravity { get; set; }

        int IslandIndex { get; set; }

        /// <summary>
        ///     Has this body already been added to a physics island
        /// </summary>
        bool Island { get; set; }

        /// <summary>
        ///     Should the body still have physics updates applied even if paused
        /// </summary>
        bool IgnorePaused { get; set; }

        /// <summary>
        ///     Entity that this physBody represents.
        /// </summary>
        IEntity Entity { get; }

        /// <summary>
        ///     AABB of this entity in world space.
        /// </summary>
        Box2 GetWorldAABB(Vector2? worldPosition = null, Angle? worldRotation = null);

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

        void CreateProxies(IMapManager? mapManager = null, SharedBroadPhaseSystem? broadPhaseSystem = null);

        void ClearProxies();

        /// <summary>
        ///     Removes all of the currently active contacts for this body.
        /// </summary>
        void DestroyContacts();

        IReadOnlyList<Fixture> Fixtures { get; }

        /// <summary>
        ///     The map index this physBody is located upon
        /// </summary>
        MapId MapID { get; }

        /// <summary>
        /// The type of the body, which determines how collisions effect this object.
        /// </summary>
        BodyType BodyType { get; set; }

        /// <summary>
        ///     Whether the body is affected by tile friction or not.
        /// </summary>
        BodyStatus BodyStatus { get; set; }

        bool Awake { get; set; }

        bool SleepingAllowed { get; set; }

        float SleepTime { get; set; }

        float LinearDamping { get; set; }

        float AngularDamping { get; set; }

        /// <summary>
        ///     Non-hard <see cref="IPhysBody"/>s will not cause action collision (e.g. blocking of movement)
        ///     while still raising collision events.
        /// </summary>
        /// <remarks>
        ///     This is useful for triggers or such to detect collision without actually causing a blockage.
        /// </remarks>
        bool Hard { get; set; }

        /// <summary>
        ///     Inverse mass of the entity in kilograms (1 / Mass).
        /// </summary>
        float InvMass { get; }

        /// <summary>
        /// Mass of the entity in kilograms
        /// Set this via fixtures.
        /// </summary>
        float Mass { get; }

        /// <summary>
        ///     Inverse inertia
        /// </summary>
        float InvI { get; set; }

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

        /// <summary>
        ///     Current linear velocity of the entity in meters per second.
        /// </summary>
        Vector2 LinearVelocity { get; set; }

        /// <summary>
        ///     Current angular velocity of the entity in radians per sec.
        /// </summary>
        float AngularVelocity { get; set; }

        /// <summary>
        /// Current position of the body in the world, in meters.
        /// </summary>
        Vector2 WorldPosition
        {
            get => Entity.Transform.WorldPosition;
            set => Entity.Transform.WorldPosition = value;
        }

        /// <summary>
        /// Current rotation of the body in the world, in radians.
        /// </summary>
        float WorldRotation
        {
            get => (float) Entity.Transform.WorldRotation.Theta;
            set => Entity.Transform.WorldRotation = new Angle(value);
        }

        void WakeBody();

        void ApplyLinearImpulse(in Vector2 impulse);

        void ApplyAngularImpulse(float impulse);

        IEnumerable<IPhysBody> GetCollidingEntities(Vector2 offset, bool approx = true);
    }
}
