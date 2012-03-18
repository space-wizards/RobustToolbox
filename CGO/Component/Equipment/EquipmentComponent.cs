using System.Collections.Generic;
using ClientInterfaces.GOC;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    public class EquipmentComponent : GameObjectComponent
    {
        public Dictionary<EquipmentSlot, IEntity> EquippedEntities = new Dictionary<EquipmentSlot, IEntity>();
        public List<EquipmentSlot> ActiveSlots = new List<EquipmentSlot>();

        public override ComponentFamily Family
        {
            get { return ComponentFamily.Equipment; }
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

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

            switch(type)
            {
                case ComponentMessageType.GetItemInEquipmentSlot:
                    reply = !IsEmpty((EquipmentSlot) list[0])
                                  ? new ComponentReplyMessage(ComponentMessageType.ReturnItemInEquipmentSlot,
                                                              EquippedEntities[(EquipmentSlot) list[0]])
                                  : new ComponentReplyMessage(ComponentMessageType.ItemSlotEmpty);
                    break;
            }

            return reply;
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
                UnEquipItem(part, EquippedEntities[part].Uid);
            }
            EquippedEntities.Add(part, EntityManager.Singleton.GetEntity(uid));
        }

        private void UnEquipItem(EquipmentSlot part, int uid)
        {
            EquippedEntities.Remove(part);
        }

        public bool IsEmpty(EquipmentSlot part)
        {
            if (EquippedEntities.ContainsKey(part))
                return false;
            return true;
        }
    }
}
