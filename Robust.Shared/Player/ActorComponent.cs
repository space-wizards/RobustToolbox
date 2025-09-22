using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Player;

[RegisterComponent]
public sealed partial class ActorComponent : Component
{
    [ViewVariables]
    public ICommonSession PlayerSession { get; internal set; } = default!;
}
