using Robust.Shared.GameObjects;
using Robust.Shared.Physics;

namespace Robust.Client.GameObjects
{
    [RegisterComponent]
    public sealed class RenderingTreeComponent : Component
    {
        internal DynamicTree<ComponentTreeEntry<SpriteComponent>> SpriteTree { get; set; } = default!;
        internal DynamicTree<ComponentTreeEntry<PointLightComponent>> LightTree { get; set; } = default!;
    }
}
