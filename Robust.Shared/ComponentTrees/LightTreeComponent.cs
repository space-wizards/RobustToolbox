using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.ComponentTrees;

[RegisterComponent]
public sealed partial class LightTreeComponent : Component, IComponentTreeComponent<SharedPointLightComponent>
{
    [ViewVariables]
    public DynamicTree<ComponentTreeEntry<SharedPointLightComponent>> Tree { get; set; } = default!;
}
