using System;
using SS13_Shared.Serialization;

namespace SS13_Shared.GameStates
{
    [Serializable]
    public class PlayerState : INetSerializableType
    {
        public int? ControlledEntity;
        public string Name;
        public SessionStatus Status;
        public long UniqueIdentifier;
    }
}