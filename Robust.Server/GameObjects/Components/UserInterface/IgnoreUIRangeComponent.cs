using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects;

/// <summary>
///     Lets any entities with this component ignore user interface range checks that would normally
///     close the UI automatically.
/// </summary>
[RegisterComponent]
public sealed partial class IgnoreUIRangeComponent : Component
{
}
