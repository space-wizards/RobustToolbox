using System.Collections.Generic;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;

namespace Robust.Server.GameObjects
{
    [RegisterComponent]
    internal sealed partial class ViewSubscriberComponent : Component
    {
        internal readonly HashSet<ICommonSession> SubscribedSessions = new();
    }
}
