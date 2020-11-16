using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Shared.Interfaces.Physics
{
    /// <summary>
    ///     This service provides access into the physics system.
    /// </summary>
    public interface IPhysicsManager
    {
        /// <summary>
        /// Checks to see if the specified collision rectangle collides with any of the physBodies under management.
        /// Also fires the OnCollide event of the first managed physBody to intersect with the collider.
        /// </summary>
        /// <param name="collider">Collision rectangle to check</param>
        /// <param name="map">Map to check on</param>
        /// <returns>true if collides, false if not</returns>
        bool TryCollideRect(Box2 collider, MapId map);

        /// <summary>
        ///     Checks whether a certain grid position is weightless or not
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        bool IsWeightless(EntityCoordinates coordinates);

        /// <summary>
        /// Get all entities colliding with a certain body.
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        IEnumerable<IEntity> GetCollidingEntities(IPhysBody body, Vector2 offset, bool approximate = true);

        IEnumerable<IPhysBody> GetCollidingEntities(MapId mapId, in Box2 worldBox);

        /// <summary>
        ///     Checks whether a body is colliding
        /// </summary>
        /// <param name="body"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        bool IsColliding(IPhysBody body, Vector2 offset, bool approx);

        void AddBody(IPhysBody physBody);
        void RemoveBody(IPhysBody physBody);

        /// <summary>
        ///     Casts a ray in the world and returns the first entity it hits, or a list of all entities it hits.
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="ray">Ray to cast in the world.</param>
        /// <param name="maxLength">Maximum length of the ray in meters.</param>
        /// <param name="ignoredEnt">A single entity that can be ignored by the RayCast. Useful if the ray starts inside the body of an entity.</param>
        /// <param name="returnOnFirstHit">If false, will return a list of everything it hits, otherwise will just return a list of the first entity hit</param>
        /// <returns>An enumerable of either the first entity hit or everything hit</returns>
        IEnumerable<RayCastResults> IntersectRay(MapId mapId, CollisionRay ray, float maxLength = 50, IEntity? ignoredEnt = null, bool returnOnFirstHit = true);


        /// <summary>
        ///     Casts a ray in the world and returns the distance the ray traveled while colliding with entities
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="ray">Ray to cast in the world.</param>
        /// <param name="maxLength">Maximum length of the ray in meters.</param>
        /// <param name="ignoredEnt">A single entity that can be ignored by the RayCast. Useful if the ray starts inside the body of an entity.</param>
        /// <returns>The distance the ray traveled while colliding with entities</returns>
        public float IntersectRayPenetration(MapId mapId, CollisionRay ray, float maxLength, IEntity? ignoredEnt = null);

        /// <summary>
        ///     Calculates the penetration depth of the axis-of-least-penetration for a
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        float CalculatePenetration(IPhysBody target, IPhysBody source);

        Vector2 SolveCollisionImpulse(Manifold manifold);

        /// <summary>
        ///     Casts a ray in the world, returning the first entity it hits (or all entities it hits, if so specified)
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="ray">Ray to cast in the world.</param>
        /// <param name="maxLength">Maximum length of the ray in meters.</param>
        /// <param name="predicate">A predicate to check whether to ignore an entity or not. If it returns true, it will be ignored.</param>
        /// <param name="returnOnFirstHit">If true, will only include the first hit entity in results. Otherwise, returns all of them.</param>
        /// <returns>A result object describing the hit, if any.</returns>
        IEnumerable<RayCastResults> IntersectRayWithPredicate(MapId mapId, CollisionRay ray, float maxLength = 50, Func<IEntity, bool>? predicate = null, bool returnOnFirstHit = true);

        event Action<DebugRayData> DebugDrawRay;

        bool Update(IPhysBody collider);

        void RemovedFromMap(IPhysBody body, MapId mapId);
        void AddedToMap(IPhysBody body, MapId mapId);
        int SleepTimeThreshold { get; set; }
    }

    public struct DebugRayData
    {
        public DebugRayData(Ray ray, float maxLength, [CanBeNull] RayCastResults? results)
        {
            Ray = ray;
            MaxLength = maxLength;
            Results = results;
        }

        public Ray Ray
        {
            get;
        }

        public RayCastResults? Results { get; }
        public float MaxLength { get; }
    }

    public readonly struct Manifold
    {
        public readonly IPhysicsComponent A;
        public readonly IPhysicsComponent B;

        public readonly Vector2 Normal;
        public readonly bool Hard;

        public Vector2 RelativeVelocity => B.LinearVelocity - A.LinearVelocity;

        public bool Unresolved => Vector2.Dot(RelativeVelocity, Normal) < 0 && Hard;

        public Manifold(IPhysicsComponent a, IPhysicsComponent b, bool hard)
        {
            A = a;
            B = b;
            Normal = PhysicsManager.CalculateNormal(a, b);
            Hard = hard;
        }
    }
}
