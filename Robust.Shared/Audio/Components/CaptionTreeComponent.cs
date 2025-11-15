using Robust.Shared.Physics;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Audio.Components;

/// <summary>
/// Samples nearby <see cref="CaptionComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CaptionTreeComponent : Component, IComponentTreeComponent<CaptionComponent>
{
    public DynamicTree<ComponentTreeEntry<CaptionComponent>> Tree { get; set; } = default!;
}
