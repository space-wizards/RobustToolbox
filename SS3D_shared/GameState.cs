using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using SS13_Shared.GO;

namespace SS13_Shared
{
    public class GameState
    {
        public uint Sequence { get; private set;}
        public List<EntityState> EntityStates { get; set; }

        public GameState(uint sequence)
        {
            Sequence = sequence;
        }

        public NetOutgoingMessage GameStateUpdate(NetOutgoingMessage StateUpdateMessage)
        {
            StateUpdateMessage.Write(Sequence);
            return StateUpdateMessage;
        }

        public NetOutgoingMessage WriteDelta(NetOutgoingMessage StateDeltaMessage)
        {
            return StateDeltaMessage;
        }
    }
}
