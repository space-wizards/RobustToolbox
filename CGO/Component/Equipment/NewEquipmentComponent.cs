using System.Collections.Generic;
using GameObject;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    public class NewEquipmentComponent : Component
    {
        public List<EquipmentSlot> ActiveSlots = new List<EquipmentSlot>();
        public Dictionary<EquipmentSlot, Entity> EquippedEntities = new Dictionary<EquipmentSlot, Entity>();

        public NewEquipmentComponent()
        {
            Family = ComponentFamily.Equipment;
        }
    }
}