using Lidgren.Network;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Equipment;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.GameObjects
{
    [IoCTarget]
    public class EquipmentComponent : ClientComponent
    {
        public override string Name => "Equipment";
        public List<EquipmentSlot> ActiveSlots = new List<EquipmentSlot>();
        public Dictionary<EquipmentSlot, Entity> EquippedEntities = new Dictionary<EquipmentSlot, Entity>();

        public EquipmentComponent()
        {
            Family = ComponentFamily.Equipment;
        }

        public override Type StateType
        {
            get { return typeof(EquipmentComponentState); }
        }

        public void DispatchEquip(int uid)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered, ComponentMessageType.EquipItem,
                                              uid);
        }

        public void DispatchEquipFromHand()
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered,
                                              ComponentMessageType.EquipItemInHand);
        }

        public void DispatchUnEquipToHand(int uid)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered,
                                              ComponentMessageType.UnEquipItemToHand, uid);
        }

        public void DispatchUnEquipItemToSpecifiedHand(int uid, InventoryLocation hand)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered,
                                              ComponentMessageType.UnEquipItemToSpecifiedHand, uid, hand);
        }

        public void DispatchUnEquipToFloor(int uid)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered,
                                              ComponentMessageType.UnEquipItemToFloor, uid);
        }

        private void EquipItem(EquipmentSlot part, int uid)
        {
            if (!IsEmpty(part))
            // Uh oh we are confused about something! But it's better to just do what the server says
            {
                UnEquipItem(part);
            }
            EquippedEntities.Add(part, Owner.EntityManager.GetEntity(uid));
        }

        private void UnEquipItem(EquipmentSlot part)
        {
            EquippedEntities.Remove(part);
        }

        public void UnEquipItem(Entity entity)
        {
            if (EquippedEntities.ContainsValue(entity))
                EquippedEntities.Remove(EquippedEntities.Where(x => x.Value == entity).Select(x => x.Key).First());
        }

        public bool IsEmpty(EquipmentSlot part)
        {
            if (EquippedEntities.ContainsKey(part))
                return false;
            return true;
        }

        public bool IsEquipped(Entity entity, EquipmentSlot slot)
        {
            return EquippedEntities.ContainsKey(slot) && EquippedEntities[slot] == entity;
        }

        public bool IsEquipped(Entity entity)
        {
            return EquippedEntities.ContainsValue(entity);
        }

        public override void HandleComponentState(dynamic state)
        {
            foreach (KeyValuePair<EquipmentSlot, int> curr in state.EquippedEntities)
            {
                Entity retEnt = Owner.EntityManager.GetEntity(curr.Value);
                if (retEnt == null && !IsEmpty(curr.Key))
                {
                    UnEquipItem(curr.Key);
                }
                else if (retEnt != null)
                {
                    if (!IsEquipped(retEnt, curr.Key))
                    {
                        if (IsEquipped(retEnt))
                        {
                            UnEquipItem(retEnt);
                        }
                        EquipItem(curr.Key, retEnt.Uid);
                    }
                }
            }

            var removed = EquippedEntities.Keys.Where(x => !state.EquippedEntities.ContainsKey(x)).ToArray();
            foreach (EquipmentSlot rem in removed)
            {
                UnEquipItem(rem);
            }

            //Find differences and raise event?
            ActiveSlots = state.ActiveSlots;
        }
    }
}
