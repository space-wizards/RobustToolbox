using System.Collections.Generic;
using SS13_Shared.GameStates;

namespace ClientInterfaces.GameStates
{
    public interface IGameStateManager: IDictionary<uint, GameState>
    {
        void Cull();
        uint OldestStateAcked { get; }
        void Ack(long uniqueIdentifier, uint state);
    }
}
