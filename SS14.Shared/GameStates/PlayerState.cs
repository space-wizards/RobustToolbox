using SS14.Shared.Serialization;
using System;

namespace SS14.Shared.GameStates
{
    [Serializable, NetSerializable]
    public class PlayerState
    {
        public int? ControlledEntity;
        public string Name;
        public SessionStatus Status;
        public long UniqueIdentifier;
    }
}