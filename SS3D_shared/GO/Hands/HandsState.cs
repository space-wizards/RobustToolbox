using System;
using System.Collections.Generic;
using SS13_Shared.Serialization;

namespace SS13_Shared.GO.Hands
{
    [Serializable]
    public class HandsState : ComponentState 
    {
        public InventoryLocation ActiveHand;
        public Dictionary<InventoryLocation, int> Slots;

        public HandsState(InventoryLocation _ActiveHand, Dictionary<InventoryLocation, int> _Slots) 
            : base(ComponentFamily.Hands)
        {
            ActiveHand = _ActiveHand;
            Slots = _Slots;
        }
    }
}