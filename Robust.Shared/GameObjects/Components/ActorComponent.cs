using Robust.Shared.GameStates;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Marks a session as controlling this entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActorComponent : Component
{
    [ViewVariables]
    public ICommonSession Session { get; internal set; } = default!;
}
