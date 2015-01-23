using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using System.Collections.Generic;

namespace SS14.Client.GameObjects
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