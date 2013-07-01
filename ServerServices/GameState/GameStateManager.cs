using System.Collections.Generic;
using System.Linq;
using ServerInterfaces.GameState;

namespace ServerServices.GameState
{
    public class GameStateManager : Dictionary<uint, SS13_Shared.GameState>, IGameStateManager
    {
        private Dictionary<long, uint> ackedStates = new Dictionary<long, uint>();  

        public GameStateManager()
        {
            
        }

        public void Cull()
        {
            foreach (var v in Keys.Where( v => v < OldestStateAcked).ToList())
                Remove(v);
        }

        public void CullAll()
        {
            Clear();
        }

        public uint OldestStateAcked
        {
            get
            {
                var state = ackedStates.Values.FirstOrDefault(val => val == ackedStates.Values.Max());
                return state;
            }
        }

        public void Ack(long uniqueIdentifier, uint stateAcked)
        {
            if(!ackedStates.ContainsKey(uniqueIdentifier))
                ackedStates.Add(uniqueIdentifier, stateAcked);
            else
                ackedStates[uniqueIdentifier] = stateAcked;
        }
    }
}
