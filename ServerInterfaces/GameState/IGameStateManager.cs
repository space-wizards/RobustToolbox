using System.Collections.Generic;

namespace ServerInterfaces.GameState
{
    public interface IGameStateManager: IDictionary<uint, SS13_Shared.GameState>
    {
        void Cull();
        uint OldestStateAcked { get; }
        void Ack(long uniqueIdentifier, uint state);
    }
}
