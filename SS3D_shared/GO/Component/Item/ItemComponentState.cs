using System;

namespace SS13_Shared.GO.Component.Item
{
    [Serializable]
    public class ItemComponentState : ComponentState
    {
        public int? Holder;
        public InventoryLocation InventoryLocation;

        public ItemComponentState(int? holder, InventoryLocation inventoryLocation)
            :base(ComponentFamily.Item)
        {
            Holder = holder;
            InventoryLocation = inventoryLocation;
            Family = ComponentFamily.Item;
        }
    }
}
