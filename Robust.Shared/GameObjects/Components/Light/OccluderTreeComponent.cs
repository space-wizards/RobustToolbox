using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Stores the relevant occluder children of this entity.
/// </summary>
[RegisterComponent]
public sealed partial class OccluderTreeComponent : Component, IComponentTreeComponent<OccluderComponent>
{
    public DynamicTree<ComponentTreeEntry<OccluderComponent>> Tree { get; set; } = default!;
}
