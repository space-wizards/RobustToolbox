using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    [RegisterComponent]
    public sealed partial class ActiveUserInterfaceComponent : Component
    {
        [ViewVariables]
        public HashSet<PlayerBoundUserInterface> Interfaces = new();
    }

    [PublicAPI]
    public sealed class ServerBoundUserInterfaceMessage
    {
        [ViewVariables]
        public BoundUserInterfaceMessage Message { get; }
        [ViewVariables]
        public ICommonSession Session { get; }

        public ServerBoundUserInterfaceMessage(BoundUserInterfaceMessage message, ICommonSession session)
        {
            Message = message;
            Session = session;
        }
    }
}
