using System.Collections.Generic;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Maths;
using SS14.Shared.Physics;

namespace SS14.Shared.Interfaces.Physics
{
    public interface ICollisionManager
    {
        /// <summary>
        /// Checks to see if the specified collision rectangle collides with any of the colliders under management.
        /// Also fires the OnCollide event of the first managed collidable to intersect with the collider.
        /// </summary>
        /// <param name="collider">Collision rectangle to check</param>
        /// <returns>true if collides, false if not</returns>
        bool IsColliding(Box2 collider);

        bool TryCollide(IEntity collider);
        bool TryCollide(IEntity collider, Vector2 offset, bool bump = true);
        void AddCollidable(ICollidable collidable);
        void RemoveCollidable(ICollidable collidable);
        void UpdateCollidable(ICollidable collidable);

        /// <summary>
        ///     Casts a ray in the world and returns the first thing it hit.
        /// </summary>
        /// <param name="ray">Ray to cast in the world.</param>
        /// <param name="maxLength">Maximum length of the ray in meters.</param>
        /// <returns>Owning entity of the object that was hit, or null if nothing was hit.</returns>
        RayCastResults IntersectRay(Ray ray, float maxLength = 50);
    }
}
