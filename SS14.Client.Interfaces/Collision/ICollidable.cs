using SS14.Shared.GameObjects;
using System.Drawing;

namespace SS14.Client.Interfaces.Collision
{
    public interface ICollidable
    {
        RectangleF AABB { get; }
        bool IsHardCollidable { get; } // true if collisions should prevent movement, or just trigger bumps.
        void Bump(Entity ent);
    }
}