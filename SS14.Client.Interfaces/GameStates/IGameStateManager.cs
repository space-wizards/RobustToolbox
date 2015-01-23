using SS14.Shared.GameStates;
using System.Collections.Generic;

namespace SS14.Client.Interfaces.GameStates
{
    public interface IGameStateManager : IDictionary<uint, GameState>
    {
        uint OldestStateAcked { get; }
        void Cull();
        void Ack(long uniqueIdentifier, uint state);
    }
}