using Robust.Server.GameObjects;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.ViewVariables;

namespace Robust.Server.ComponentTrees
{
    [RegisterComponent]
    public sealed partial class LightTreeComponent : SharedLightTreeComponent, IComponentTreeComponent<PointLightComponent>
    {
        [ViewVariables]
        public DynamicTree<ComponentTreeEntry<PointLightComponent>> Tree { get; set; } = default!;
    }
}