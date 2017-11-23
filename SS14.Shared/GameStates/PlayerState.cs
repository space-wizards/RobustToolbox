using SS14.Shared.Serialization;
using System;
using SS14.Shared.Players;

namespace SS14.Shared.GameStates
{
    [Serializable, NetSerializable]
    public class PlayerState
    {
        public int? ControlledEntity;
        public string Name;
        public SessionStatus Status;
        public long UniqueIdentifier;

        public PlayerIndex Index;
    }
}
