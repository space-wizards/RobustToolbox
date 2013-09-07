using System.Collections.Generic;
using System.Linq;
using GameObject;
using Lidgren.Network;
using SGO.Item.ItemCapability;
using SS13_Shared;
using SS13_Shared.GO;

namespace SGO
{
    public class NewEquipmentComponent : Component
    {
        protected List<EquipmentSlot> activeSlots = new List<EquipmentSlot>();
        public Dictionary<EquipmentSlot, Entity> equippedEntities = new Dictionary<EquipmentSlot, Entity>();

        public NewEquipmentComponent()
        {
            Family = ComponentFamily.Equipment;
        }

        //Handle possible slots with extended parameters.
    }
}