using SS14.Shared.Serialization;
using System;

namespace SS14.Shared.GameStates
{
    [Serializable, NetSerializable]
    public class PlayerState
    {
        private int? controlledEntity;
        private string name;
        private SessionStatus status;
        private long uniqueIdentifier;

        public int? ControlledEntity { get => controlledEntity; set => controlledEntity = value; }
        public string Name { get => name; set => name = value; }
        public SessionStatus Status { get => status; set => status = value; }
        public long UniqueIdentifier { get => uniqueIdentifier; set => uniqueIdentifier = value; }
    }
}
