using System.Collections.Generic;
using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
{
    [RegisterComponent]
    internal sealed class ViewSubscriberComponent : Component
    {
        internal readonly HashSet<IPlayerSession> SubscribedSessions = new();
    }
}
