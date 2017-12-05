using SS14.Shared.Serialization;
using System;

namespace SS14.Shared.GameStates
{
    [Serializable, NetSerializable]
    public class PlayerState
    {
        public int? ControlledEntity { get; set; }
        public string Name { get; set; }
        public SessionStatus Status { get; set; }
        public long UniqueIdentifier { get; set; }
    }
}
