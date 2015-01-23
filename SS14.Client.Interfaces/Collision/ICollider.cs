using System.Drawing;

namespace SS14.Client.Interfaces.Collision
{
    public interface ICollider
    {
        RectangleF AABB { get; }
    }
}