using Robust.Shared.ComponentTrees;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;

namespace Robust.Client.ComponentTrees;

[RegisterComponent]
public sealed partial class LightTreeComponent: Component, IComponentTreeComponent<SharedPointLightComponent>
{
    public DynamicTree<ComponentTreeEntry<SharedPointLightComponent>> Tree { get; set; } = default!;
}
