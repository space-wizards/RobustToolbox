using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Robust.Server.GameObjects
{
    [RegisterComponent]
    internal sealed partial class ViewSubscriberComponent : Component
    {
        internal readonly HashSet<ICommonSession> SubscribedSessions = new();
    }
}
