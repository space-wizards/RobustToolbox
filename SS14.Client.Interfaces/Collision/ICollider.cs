using SFML.Graphics;
using System.Drawing;

namespace SS14.Client.Interfaces.Collision
{
    public interface ICollider
    {
        FloatRect AABB { get; }
    }
}