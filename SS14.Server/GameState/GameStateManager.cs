using SS14.Server.Interfaces.GameState;
using SS14.Shared;
using SS14.Shared.IoC;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Network;

namespace SS14.Server.GameStates
{
    public class GameStateManager : Dictionary<uint, SS14.Shared.GameStates.GameState>, IGameStateManager
    {
        private readonly Dictionary<long, uint> ackedStates = new Dictionary<long, uint>();

        #region IGameStateManager Members

        public void Cull()
        {
            foreach (uint v in Keys.Where(v => v < OldestStateAcked).ToList())
                Remove(v);
        }

        public uint OldestStateAcked
        {
            get
            {
                uint state = ackedStates.Values.FirstOrDefault(val => val == ackedStates.Values.Min());
                return state;
            }
        }

        public void Ack(long uniqueIdentifier, uint stateAcked)
        {
            if (!ackedStates.ContainsKey(uniqueIdentifier))
                ackedStates.Add(uniqueIdentifier, stateAcked);
            else
                ackedStates[uniqueIdentifier] = stateAcked;
        }

        public GameStateDelta GetDelta(INetChannel client, uint state)
        {
            SS14.Shared.GameStates.GameState toState = GetFullState(state);
            if (!ackedStates.ContainsKey(client.ConnectionId))
                return toState - new SS14.Shared.GameStates.GameState(0); //The client has no state!

            var ackack = ackedStates[client.ConnectionId];
            SS14.Shared.GameStates.GameState fromState = this[ackack];
            return toState - fromState;
        }

        public SS14.Shared.GameStates.GameState GetFullState(uint state)
        {
            if (ContainsKey(state))
                return this[state];
            return null; //TODO SHIT
        }

        public uint GetLastStateAcked(INetChannel client)
        {
            if (!ackedStates.ContainsKey(client.ConnectionId))
            {
                ackedStates[client.ConnectionId] = 0;
            }

            return ackedStates[client.ConnectionId];
        }

        public void CullAll()
        {
            ackedStates.Clear();
            Clear();
        }

        #endregion IGameStateManager Members
    }
}
