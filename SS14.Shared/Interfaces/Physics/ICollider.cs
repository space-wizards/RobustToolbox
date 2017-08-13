using SFML.Graphics;

namespace SS14.Shared.Interfaces.Physics
{
    public interface ICollider
    {
        FloatRect AABB { get; }
    }
}