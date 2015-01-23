using SS14.Shared.Serialization;
using System;

namespace SS14.Shared.GameStates
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