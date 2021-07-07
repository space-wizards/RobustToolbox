using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects
{
    public class ActorComponent : Component
    {
        public override string Name => "Actor";

        [ViewVariables]
        public IPlayerSession PlayerSession { get; internal set; } = default!;
    }
}
