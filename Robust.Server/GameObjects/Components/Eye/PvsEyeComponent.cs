using System.Collections.Generic;
using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
{
    [RegisterComponent]
    internal class PvsEyeComponent : Component
    {
        public override string Name => "PvsEye";

        internal readonly HashSet<IPlayerSession> SubscribedSessions = new();
    }
}
