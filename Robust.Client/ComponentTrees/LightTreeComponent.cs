using Robust.Client.GameObjects;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;

namespace Robust.Client.ComponentTrees;

public sealed class LightTreeComponent: Component, IComponentTreeComponent<PointLightComponent>
{
    public DynamicTree<ComponentTreeEntry<PointLightComponent>> Tree { get; set; } = default!;
}
