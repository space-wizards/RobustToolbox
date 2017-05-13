using Lidgren.Network;
using SS14.Shared;
using System.Collections.Generic;
using SS14.Shared.IoC;

namespace SS14.Server.Interfaces.GameState
{
    public interface IGameStateManager : IDictionary<uint, SS14.Shared.GameStates.GameState>, IIoCInterface
    {
        uint OldestStateAcked { get; }
        void Cull();
        void Ack(long uniqueIdentifier, uint state);
        GameStateDelta GetDelta(NetConnection client, uint state);
        SS14.Shared.GameStates.GameState GetFullState(uint state);
        uint GetLastStateAcked(NetConnection client);
    }
}
