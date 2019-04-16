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
        /// Checks to see if the specified collision rectangle collides with any of the colliders under management.
        /// Also fires the OnCollide event of the first managed collidable to intersect with the collider.
        /// </summary>
        /// <param name="collider">Collision rectangle to check</param>
        /// <param name="map">Map to check on</param>
        /// <returns>true if collides, false if not</returns>
        bool IsColliding(Box2 collider, MapId map);

        bool TryCollide(IEntity entity, Vector2 offset, bool bump = true);

        void AddCollidable(ICollidable collidable);
        void RemoveCollidable(ICollidable collidable);
        void BuildCollisionGrid();

        /// <summary>
        ///     Casts a ray in the world and returns the first thing it hit.
        /// </summary>
        /// <param name="ray">Ray to cast in the world.</param>
        /// <param name="maxLength">Maximum length of the ray in meters.</param>
        /// <param name="ignoredEnt">A single entity that can be ignored by the RayCast. Useful if the ray starts inside the body of an entity.</param>
        /// <returns>A result object describing the hit, if any.</returns>
        RayCastResults IntersectRay(Ray ray, float maxLength = 50, IEntity ignoredEnt = null);
    }
}
