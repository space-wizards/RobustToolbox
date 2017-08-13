using SFML.Graphics;
using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Shared.Interfaces.Physics
{
    public interface ICollidable
    {
        FloatRect WorldAABB { get; }
        FloatRect AABB { get; }
        bool IsHardCollidable { get; } // true if collisions should prevent movement, or just trigger bumps.
        void Bump(IEntity ent);
    }
}
