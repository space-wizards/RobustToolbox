using System.Collections.Generic;
using Lidgren.Network;
using SS13_Shared;

namespace ServerInterfaces.GameState
{
    public interface IGameStateManager : IDictionary<uint, SS13_Shared.GameStates.GameState>
    {
        uint OldestStateAcked { get; }
        void Cull();
        void Ack(long uniqueIdentifier, uint state);
        GameStateDelta GetDelta(NetConnection client, uint state);
        SS13_Shared.GameStates.GameState GetFullState(uint state);
        uint GetLastStateAcked(NetConnection client);
    }
}