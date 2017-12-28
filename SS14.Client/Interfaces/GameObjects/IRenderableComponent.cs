using OpenTK;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.Interfaces.GameObjects
{
    public interface IRenderableComponent : IComponent
    {
        DrawDepth DrawDepth { get; set; }
        float Bottom { get; }
        Box2 LocalAABB { get; }
        Box2 AverageAABB { get; }
        int MapID { get; }
    }
}
