using Robust.Shared.GameObjects;
using Robust.Shared.Physics;

namespace Robust.Client.GameObjects
{
    [RegisterComponent]
    public sealed class RenderingTreeComponent : Component
    {
        internal DynamicTree<SpriteComponent> SpriteTree { get; set; } = default!;
        internal DynamicTree<PointLightComponent> LightTree { get; set; } = default!;
    }
}
