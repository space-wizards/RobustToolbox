using Robust.Client.GameObjects;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;

namespace Robust.Client.ComponentTrees;

[RegisterComponent]
public sealed partial class SpriteTreeComponent: Component, IComponentTreeComponent<SpriteComponent>
{
    public DynamicTree<ComponentTreeEntry<SpriteComponent>> Tree { get; set; } = default!;
}
