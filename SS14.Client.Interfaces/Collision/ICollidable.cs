using SFML.Graphics;
using SS14.Shared.GameObjects;

namespace SS14.Client.Interfaces.Collision
{
    public interface ICollidable
    {
        FloatRect AABB { get; }
        bool IsHardCollidable { get; } // true if collisions should prevent movement, or just trigger bumps.
        void Bump(Entity ent);
    }
}