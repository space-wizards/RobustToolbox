using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.GameObjects
{
    public interface IRenderableComponent : IComponent
    {
        DrawDepth DrawDepth { get; set; }
        float Bottom { get; }
        Box2 LocalAABB { get; }
        Box2 AverageAABB { get; }
        MapId MapID { get; }
    }
}
