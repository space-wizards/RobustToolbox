using System.Collections.Generic;
using SS13_Shared.GameStates;

namespace ClientInterfaces.GameStates
{
    public interface IGameStateManager : IDictionary<uint, GameState>
    {
        uint OldestStateAcked { get; }
        void Cull();
        void Ack(long uniqueIdentifier, uint state);
    }
}