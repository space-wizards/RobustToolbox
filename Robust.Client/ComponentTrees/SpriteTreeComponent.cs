using System.Collections.Generic;
using Robust.Client.GameObjects;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;

namespace Robust.Client.ComponentTrees;

[RegisterComponent]
public sealed partial class SpriteTreeComponent : Component, ILayeredComponentTreeComponent<SpriteComponent>
{
    public DynamicTree<ComponentTreeEntry<SpriteComponent>> Tree { get; set; } = default!;
    public Dictionary<int, DynamicTree<ComponentTreeEntry<SpriteComponent>>> Trees { get; set; } = default!;
}
