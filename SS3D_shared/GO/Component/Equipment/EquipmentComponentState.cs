using System;
using System.Collections.Generic;
using SS13_Shared.Serialization;

namespace SS13_Shared.GO.Component.Equipment
{
    [Serializable]
    public class EquipmentComponentState : ComponentState
    {
        public Dictionary<EquipmentSlot, int> EquippedEntities;
        public List<EquipmentSlot> ActiveSlots;

        public EquipmentComponentState(Dictionary<EquipmentSlot, int> _EquippedEntities, List<EquipmentSlot> _ActiveSlots)
            : base(ComponentFamily.Equipment)
        {
            EquippedEntities = _EquippedEntities;
            ActiveSlots = _ActiveSlots;
        }
    }
}