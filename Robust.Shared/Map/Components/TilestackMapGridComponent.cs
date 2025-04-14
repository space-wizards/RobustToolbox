using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Map.Components;

/// <summary>
///     Stores tilestacks by grid indices for tiles.
///     This is only used in content for tiles that are placed not on their BaseTurfs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TilestackMapGridComponent : Component
{
    [ViewVariables]
    public Dictionary<Vector2i, List<Tile>> Data = new();
}
