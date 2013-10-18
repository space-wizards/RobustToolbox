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

        public override void HandleComponentState(dynamic state)
        {
            Dictionary<EquipmentSlot, Entity> newInventory = new Dictionary<EquipmentSlot, Entity>();

            foreach (KeyValuePair<EquipmentSlot, int> curr in state.EquippedEntities)
            {
                Entity retEnt = Owner.EntityManager.GetEntity(curr.Value);
                newInventory.Add(curr.Key, retEnt);
            }

            //Find differences and raise event?

            EquippedEntities = newInventory;
            ActiveSlots = state.ActiveSlots;
        }
    }
}