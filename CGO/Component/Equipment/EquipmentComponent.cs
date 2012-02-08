using System.Collections.Generic;
using ClientInterfaces.GOC;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    public class EquipmentComponent : GameObjectComponent
    {
        public Dictionary<EquipmentSlot, IEntity> equippedEntities = new Dictionary<EquipmentSlot, IEntity>();
        public List<EquipmentSlot> activeSlots = new List<EquipmentSlot>();

        public EquipmentComponent()
        {
            family = ComponentFamily.Equipment;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            switch ((EquipmentComponentNetMessage)message.MessageParameters[0])
            {
                case EquipmentComponentNetMessage.ItemEquipped:
                    EquipItem((EquipmentSlot)message.MessageParameters[1], (int)message.MessageParameters[2]);
                    break;
                case EquipmentComponentNetMessage.ItemUnEquipped:
                    UnEquipItem((EquipmentSlot)message.MessageParameters[1], (int)message.MessageParameters[2]);
                    break;
            }
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> reply, params object[] list)
        {
            base.RecieveMessage(sender, type, reply, list);

            switch(type)
            {
                case ComponentMessageType.GetItemInEquipmentSlot:
                    if (!IsEmpty((EquipmentSlot)list[0]))
                        reply.Add(new ComponentReplyMessage(ComponentMessageType.ReturnItemInEquipmentSlot, equippedEntities[(EquipmentSlot)list[0]]));
                    else
                        reply.Add(new ComponentReplyMessage(ComponentMessageType.ItemSlotEmpty));
                    break;
            }
        }

        public void DispatchEquip(int uid)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, ComponentMessageType.EquipItem, uid);
        }

        public void DispatchEquipToPart(int uid, EquipmentSlot part)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, ComponentMessageType.EquipItemToPart, uid, part);
        }

        public void DispatchEquipFromHand()
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, ComponentMessageType.EquipItemInHand);
        }

        public void DispatchUnEquipToHand(int uid)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, ComponentMessageType.UnEquipItemToHand, uid);
        }

        public void DispatchUnEquipItemToSpecifiedHand(int uid, Hand hand)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, ComponentMessageType.UnEquipItemToSpecifiedHand, uid, hand);
        }

        public void DispatchUnEquipToFloor(int uid)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, ComponentMessageType.UnEquipItemToFloor, uid);
        }

        private void EquipItem(EquipmentSlot part, int uid)
        {
            if (!IsEmpty(part)) // Uh oh we are confused about something! But it's better to just do what the server says
            {
                UnEquipItem(part, equippedEntities[part].Uid);
            }
            equippedEntities.Add(part, EntityManager.Singleton.GetEntity(uid));
        }

        private void UnEquipItem(EquipmentSlot part, int uid)
        {
            equippedEntities.Remove(part);
        }

        public bool IsEmpty(EquipmentSlot part)
        {
            if (equippedEntities.ContainsKey(part))
                return false;
            return true;
        }
    }
}
