using System.Collections.Generic;
using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
{
    [RegisterComponent]
    internal class ViewSubscriberComponent : Component
    {
        public override string Name => "ViewSubscriber";

        internal readonly HashSet<IPlayerSession> SubscribedSessions = new();
    }
}
