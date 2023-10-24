using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects
{
    [RegisterComponent]
    public sealed partial class ActorComponent : Component
    {
        [ViewVariables]
        public ICommonSession PlayerSession { get; internal set; } = default!;
    }
}
