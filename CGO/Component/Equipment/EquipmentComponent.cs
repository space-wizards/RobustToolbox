using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;

namespace CGO
{
    public class EquipmentComponent : GameObjectComponent
    {
        public Dictionary<GUIBodyPart, Entity> equippedEntities = new Dictionary<GUIBodyPart, Entity>();
        public List<GUIBodyPart> activeSlots = new List<GUIBodyPart>();

        public EquipmentComponent()
        {
            family = ComponentFamily.Equipment;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            switch((EquipmentComponentNetMessage)message.messageParameters[0])
            {
                case EquipmentComponentNetMessage.ItemEquipped:
                    EquipItem((GUIBodyPart)message.messageParameters[1], (int)message.messageParameters[2]);
                    break;
                case EquipmentComponentNetMessage.ItemUnEquipped:
                    UnEquipItem((GUIBodyPart)message.messageParameters[1], (int)message.messageParameters[2]);
                    break;
            }
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> reply, params object[] list)
        {
            base.RecieveMessage(sender, type, reply, list);

            switch(type)
            {
                case ComponentMessageType.GetItemInEquipmentSlot:
                    if (!IsEmpty((GUIBodyPart)list[0]))
                        reply.Add(new ComponentReplyMessage(ComponentMessageType.ReturnItemInEquipmentSlot, equippedEntities[(GUIBodyPart)list[0]]));
                    break;
            }
        }

        public void DispatchEquip(int uid)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, ComponentMessageType.EquipItem, uid);
        }

        public void DispatchEquipToPart(int uid, GUIBodyPart part)
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

        public void DispatchUnEquipToFloor(int uid)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, ComponentMessageType.UnEquipItemToFloor, uid);
        }



        private void EquipItem(GUIBodyPart part, int uid)
        {
            equippedEntities.Add(part, EntityManager.Singleton.GetEntity(uid));
        }

        private void UnEquipItem(GUIBodyPart part, int uid)
        {
            equippedEntities.Remove(part);
        }

        private bool IsEmpty(GUIBodyPart part)
        {
            if (equippedEntities.ContainsKey(part))
                return false;
            return true;
        }
    }
}
