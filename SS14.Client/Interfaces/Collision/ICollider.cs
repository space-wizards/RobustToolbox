using SFML.Graphics;

namespace SS14.Client.Interfaces.Collision
{
    public interface ICollider
    {
        FloatRect AABB { get; }
    }
}