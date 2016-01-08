using SS14.Shared;
using SS14.Shared.GameObjects;
using System.Drawing;
using SS14.Shared.Maths;
using SFML.Graphics;
using SFML.System;

namespace SS14.Client.Interfaces.Collision
{
    public interface ICollisionManager
    {
        /// <summary>
        /// Checks to see if the specified collision rectangle collides with any of the colliders under management.
        /// Also fires the OnCollide event of the first managed collidable to intersect with the collider.
        /// </summary>
        /// <param name="collider">Collision rectangle to check</param>
        /// <returns>true if collides, false if not</returns>
        bool IsColliding(FloatRect collider);

        bool TryCollide(Entity collider);
        bool TryCollide(Entity collider, Vector2f offset, bool bump = true);
        void AddCollidable(ICollidable collidable);
        void RemoveCollidable(ICollidable collidable);
        void UpdateCollidable(ICollidable collidable);
    }
}