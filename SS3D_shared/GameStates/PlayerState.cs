using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared.Serialization;

namespace SS13_Shared.GameStates
{
    [Serializable]
    public class PlayerState : INetSerializableType
    {
        public int? ControlledEntity;
        public SessionStatus Status;
        public string Name;
        public long UniqueIdentifier;
    }
}
