using System.Collections.Generic;
using Lidgren.Network;
using SS13_Shared;

namespace ServerInterfaces.GameState
{
    public interface IGameStateManager: IDictionary<uint, SS13_Shared.GameState>
    {
        void Cull();
        uint OldestStateAcked { get; }
        void Ack(long uniqueIdentifier, uint state);
        GameStateDelta GetDelta(NetConnection client, uint state);
        SS13_Shared.GameState GetFullState(uint state);
        uint GetLastStateAcked(NetConnection client);
    }
}
