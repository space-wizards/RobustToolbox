using System.Collections.Generic;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Network;

namespace SS14.Server.Interfaces.GameState
{
    public interface IGameStateManager : IDictionary<uint, Shared.GameStates.GameState>
    {
        uint OldestStateAcked { get; }
        void Initialize();
        void Cull();
        void Ack(long uniqueIdentifier, uint state);
        GameStateDelta GetDelta(INetChannel client, uint state);
        Shared.GameStates.GameState GetFullState(uint state);
        uint GetLastStateAcked(INetChannel client);
        void CullAll();
        void SendGameStateUpdate();
    }
}
