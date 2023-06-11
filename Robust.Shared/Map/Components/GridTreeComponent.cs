using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Map.Components;

[RegisterComponent, NetworkedComponent]
public sealed class GridTreeComponent : Component
{
    [ViewVariables]
    public readonly B2DynamicTree<(EntityUid Uid, MapGridComponent Grid)> Tree = new();
}
