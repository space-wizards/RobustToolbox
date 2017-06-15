using System;
using System.Collections.Generic;

namespace SS14.Shared.GameObjects.Components.Inventory
{
    [Serializable]
    public class InventoryComponentState : ComponentState
    {
        public int MaxSlots;
        // For some really bizarre reason I cannot fathom, if ContainedEntities is a List<int>, NetSerializer doesn't send it through properly.
        // So it's an array. Sorry.
        public int[] ContainedEntities;

        public InventoryComponentState(int maxSlots, List<int> containedEntities)
            : base (ComponentFamily.Inventory)
        {
            MaxSlots = maxSlots;
            ContainedEntities = containedEntities.ToArray();
        }
    }
}
