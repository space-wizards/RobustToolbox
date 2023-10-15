using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Tracks UIs open for a particular entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActorUIComponent : Component
{
    [ViewVariables]
    public List<PlayerBoundUserInterface> OpenBUIS = new();
}
