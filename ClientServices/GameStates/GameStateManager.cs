using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.GameStates;
using SS13_Shared;
using SS13_Shared.GameStates;

namespace ClientServices.GameStates
{
    public class GameStateManager : Dictionary<uint, GameState>, IGameStateManager
    {
        private readonly Dictionary<long, uint> ackedStates = new Dictionary<long, uint>();
        private uint currentStateSeq;
        public GameState CurrentState { get; private set; }

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
                uint state = ackedStates.Values.FirstOrDefault(val => val == ackedStates.Values.Max());
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

        #endregion

        public void CullAll()
        {
            Clear();
        }

        public void ApplyFullState(uint seq, GameState fullState)
        {
            CurrentState = fullState;
            currentStateSeq = seq;
        }

        public void ApplyDeltaState(uint oldStateSeq, uint newStateSeq, GameStateDelta delta)
        {
        }
    }
}