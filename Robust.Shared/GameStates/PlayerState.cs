using Robust.Shared.Serialization;
using System;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Network;

namespace Robust.Shared.GameStates
{
    [Serializable, NetSerializable]
    public sealed class PlayerState
    {
        public NetSessionId SessionId { get; set; }

        public string Name { get; set; }
        public SessionStatus Status { get; set; }
        public short Ping { get; set; }

        public EntityUid? ControlledEntity { get; set; }
    }
}
