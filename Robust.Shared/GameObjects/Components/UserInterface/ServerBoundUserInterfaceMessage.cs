using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Robust.Shared.GameObjects
{
    [RegisterComponent]
    public sealed partial class ActiveUserInterfaceComponent : Component
    {
        public HashSet<PlayerBoundUserInterface> Interfaces = new();
    }

    [PublicAPI]
    public sealed class ServerBoundUserInterfaceMessage
    {
        public BoundUserInterfaceMessage Message { get; }
        public ICommonSession Session { get; }

        public ServerBoundUserInterfaceMessage(BoundUserInterfaceMessage message, ICommonSession session)
        {
            Message = message;
            Session = session;
        }
    }
}
