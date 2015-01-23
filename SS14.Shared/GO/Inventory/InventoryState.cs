using System;
using System.Collections.Generic;

namespace SS14.Shared.GO.Inventory
{
    [Serializable]
    public class InventoryState : ComponentState
    {
        public int MaxSlots;
        public List<int> ContainedEntities;

        public InventoryState(int _MaxSlots, List<int> _ContainedEntities) 
            : base (ComponentFamily.Inventory)
        {
            MaxSlots = _MaxSlots;
            ContainedEntities = _ContainedEntities;
        }
    }
}