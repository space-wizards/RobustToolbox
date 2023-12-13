using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Map.Components;

/// <summary>
/// Stores what grids moved in a tick.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MovedGridsComponent : Component
{
    [ViewVariables]
    public HashSet<EntityUid> MovedGrids = new();
}
