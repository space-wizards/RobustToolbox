using SS14.Shared.Serialization;
using System;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Players;
using SS14.Shared.Network;

namespace SS14.Shared.GameStates
{
    [Serializable, NetSerializable]
    public class PlayerState
    {
        public NetSessionId SessionId { get; set; }

        public string Name { get; set; }
        public SessionStatus Status { get; set; }
        public short Ping { get; set; }

        public EntityUid? ControlledEntity { get; set; }
    }
}
