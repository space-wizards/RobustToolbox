using System;
using System.Collections.Generic;
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
        /// Get all entities colliding with a certain body.
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        IEnumerable<ICollidableComponent> GetCollidingEntities(IPhysBody body, Vector2 offset);

        /// <summary>
        ///     Checks whether a body is colliding
        /// </summary>
        /// <param name="body"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        bool IsColliding(IPhysBody body, Vector2 offset);

        void AddBody(IPhysBody physBody);
        void RemoveBody(IPhysBody physBody);

        /// <summary>
        ///     Casts a ray in the world and returns the first thing it hit.
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="ray">Ray to cast in the world.</param>
        /// <param name="maxLength">Maximum length of the ray in meters.</param>
        /// <param name="ignoredEnt">A single entity that can be ignored by the RayCast. Useful if the ray starts inside the body of an entity.</param>
        /// <param name="ignoreNonHardCollidables">If true, the RayCast will ignore any bodies that aren't hard collidables.</param>
        /// <returns>A result object describing the hit, if any.</returns>
        RayCastResults IntersectRay(MapId mapId, CollisionRay ray, float maxLength = 50, IEntity ignoredEnt = null);


        /// <summary>
        ///     Calculates the resulting impulse vector on the target collidable from the source collidable
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <param name="targetVel"></param>
        /// <param name="targetMass"></param>
        /// <param name="sourceVel"></param>
        /// <param name="sourceMass"></param>
        /// <returns>A impulse vector in kilogram meters per second</returns>
        Vector2 CalculateCollisionImpulse(ICollidableComponent target, ICollidableComponent source, Vector2 targetVel, Vector2 sourceVel, float targetMass, float sourceMass);

        /// <summary>
        ///     Casts a ray in the world and returns the first thing it hit.
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="ray">Ray to cast in the world.</param>
        /// <param name="maxLength">Maximum length of the ray in meters.</param>
        /// <param name="predicate">A predicate to check whether to ignore an entity or not. If it returns true, it will be ignored.</param>
        /// <param name="ignoreNonHardCollidables">If true, the RayCast will ignore any bodies that aren't hard collidables.</param>
        /// <returns>A result object describing the hit, if any.</returns>
        RayCastResults IntersectRayWithPredicate(MapId mapId, CollisionRay ray, float maxLength = 50, Func<IEntity, bool> predicate = null);

        event Action<DebugRayData> DebugDrawRay;

        IEnumerable<(IPhysBody, IPhysBody)> GetCollisions();

        bool Update(IPhysBody collider);

        void RemovedFromMap(IPhysBody body, MapId mapId);
        void AddedToMap(IPhysBody body, MapId mapId);
    }

    public struct DebugRayData
    {
        public DebugRayData(Ray ray, float maxLength, RayCastResults results)
        {
            Ray = ray;
            MaxLength = maxLength;
            Results = results;
        }

        public Ray Ray { get; }
        public RayCastResults Results { get; }
        public float MaxLength { get; }
    }
}
