using System;
using System.Collections.Generic;

namespace SS14.Shared.GameObjects.Components.Hands
{
    [Serializable]
    public class HandsComponentState : ComponentState
    {
        public InventoryLocation ActiveHand;
        public Dictionary<InventoryLocation, int?> Slots;

        public HandsComponentState(InventoryLocation _ActiveHand, Dictionary<InventoryLocation, int?> _Slots)
            : base(ComponentFamily.Hands)
        {
            ActiveHand = _ActiveHand;
            Slots = _Slots;
        }
    }
}
