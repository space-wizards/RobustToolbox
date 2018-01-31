using SS14.Shared.Serialization;
using System;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Players;

namespace SS14.Shared.GameStates
{
    [Serializable, NetSerializable]
    public class PlayerState
    {
        public PlayerIndex Index { get; set; }
        public long Uuid { get; set; }

        public string Name { get; set; }
        public SessionStatus Status { get; set; }
        public short Ping { get; set; }

        public EntityUid? ControlledEntity { get; set; }
    }
}
