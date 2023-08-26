using Robust.Client.GameObjects;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.ViewVariables;

namespace Robust.Client.ComponentTrees;

[RegisterComponent]
public sealed partial class LightTreeComponent: Component, IComponentTreeComponent<PointLightComponent>
{
    [ViewVariables]
    public DynamicTree<ComponentTreeEntry<PointLightComponent>> Tree { get; set; } = default!;
}
