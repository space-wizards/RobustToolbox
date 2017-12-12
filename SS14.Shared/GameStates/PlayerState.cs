using SS14.Shared.Serialization;
using System;
using SS14.Shared.Players;

namespace SS14.Shared.GameStates
{
    [Serializable, NetSerializable]
    public class PlayerState
    {
        public PlayerIndex Index;
        public long Uuid;

        public string Name;
        public SessionStatus Status;
        public short Ping;

        public int? ControlledEntity;
    }
}
