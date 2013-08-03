using System.Drawing;
using GameObject;

namespace ClientInterfaces.Collision
{
    public interface ICollidable
    {
        RectangleF AABB { get; }
        bool IsHardCollidable {get;} // true if collisions should prevent movement, or just trigger bumps.
        void Bump(Entity ent);
    }
}
