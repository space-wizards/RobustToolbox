using OpenTK;

namespace SS14.Shared.Interfaces.Physics
{
    public interface ICollider
    {
        Box2 AABB { get; }
    }
}
