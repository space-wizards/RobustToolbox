using System.Collections.Generic;
using Robust.Shared.Player;

namespace Robust.Shared.GameObjects;

// Not networked because doesn't do anything on client.
[RegisterComponent]
internal sealed partial class ViewSubscriberComponent : Component
{
    internal readonly HashSet<ICommonSession> SubscribedSessions = new();
}
