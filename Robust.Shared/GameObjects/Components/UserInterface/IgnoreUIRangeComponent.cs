using Robust.Shared.GameStates;

namespace Robust.Shared.GameObjects;

/// <summary>
///     Lets any entities with this component ignore user interface range checks that would normally
///     close the UI automatically.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class IgnoreUIRangeComponent : Component
{
}
