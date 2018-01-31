using SS14.Shared.Maths;

namespace SS14.Shared.Interfaces.Physics
{
    public interface ICollider
    {
        Box2 AABB { get; }
    }
}
