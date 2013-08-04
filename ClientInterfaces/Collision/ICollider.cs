using System.Drawing;

namespace ClientInterfaces.Collision
{
    public interface ICollider
    {
        RectangleF AABB { get; }
    }
}