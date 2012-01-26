using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;
using Lidgren.Network;

namespace SGO
{
    public class EquipmentComponent : GameObjectComponent
    {
        protected Dictionary<GUIBodyPart, Entity> equippedEntities = new Dictionary<GUIBodyPart,Entity>();
        protected List<GUIBodyPart> activeSlots = new List<GUIBodyPart>();

        public EquipmentComponent()
        {
            family = ComponentFamily.Equipment;
        }

        public override void RecieveMessage(object sender, SS3D_shared.GO.ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case ComponentMessageType.EquipItemToPart: //Equip an entity straight up.
                    EquipEntityToPart((GUIBodyPart)list[0], (Entity)list[1]);
                    break;
                case ComponentMessageType.EquipItem:
                    EquipEntity((Entity)list[0]);
                    break;
                case ComponentMessageType.EquipItemInHand: //Move an entity from a hand to an equipment slot
                    EquipEntityInHand();
                    break;
                case ComponentMessageType.UnEquipItemToFloor: //remove an entity from a slot and drop it on the floor
                    UnEquipEntity((Entity)list[0]);
                    break;
                case ComponentMessageType.UnEquipItemToHand: //remove an entity from a slot and put it in the current hand slot.
                    if (!Owner.HasComponent(SS3D_shared.GO.ComponentFamily.Hands))
                        return; //TODO REAL ERROR MESSAGE OR SOME FUCK SHIT
                    UnEquipEntityToHand((Entity)list[0]);
                    break;
            }
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            if (message.componentFamily == ComponentFamily.Equipment)
            {
                ComponentMessageType type = (ComponentMessageType)message.messageParameters[0];
                List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
                switch(type)
                {
                    case ComponentMessageType.EquipItem:
                        Owner.SendMessage(this, type, replies, EntityManager.Singleton.GetEntity((int)message.messageParameters[1]));
                        break;
                    case ComponentMessageType.EquipItemInHand:
                        Owner.SendMessage(this, type, replies, null);
                        break;
                    case ComponentMessageType.EquipItemToPart:
                        Owner.SendMessage(this, type, replies, message.messageParameters[2], EntityManager.Singleton.GetEntity((int)message.messageParameters[1]));
                        break;
                    case ComponentMessageType.UnEquipItemToFloor:
                        Owner.SendMessage(this, type, replies, EntityManager.Singleton.GetEntity((int)message.messageParameters[1]));
                        break;
                    case ComponentMessageType.UnEquipItemToHand:
                        Owner.SendMessage(this, type, replies, EntityManager.Singleton.GetEntity((int)message.messageParameters[1]));
                        break;
                }
            }
        }

        public override void HandleInstantiationMessage(Lidgren.Network.NetConnection netConnection)
        {
            foreach (GUIBodyPart p in equippedEntities.Keys)
            {
                if(!IsEmpty(p))
                {
                    Entity e = equippedEntities[p];
                    e.SendMessage(this, SS3D_shared.GO.ComponentMessageType.ItemEquipped, null, Owner);
                    Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, netConnection, EquipmentComponentNetMessage.ItemEquipped, p, e.Uid);
                }
            }
        }

        // Equips Entity e to Part part
        private void EquipEntityToPart(GUIBodyPart part, Entity e)
        {
            if (CanEquip(e)) //If the part is empty, the part exists on this mob, and the entity specified is not null
            {
                equippedEntities.Add(part, e);
                e.SendMessage(this, SS3D_shared.GO.ComponentMessageType.ItemEquipped, null, Owner);
                Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, EquipmentComponentNetMessage.ItemEquipped, part, e.Uid);
            }
        }

        // Equips Entity e and automatically finds the appropriate part
        private void EquipEntity(Entity e)
        {
            if (CanEquip(e))
            {
                List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
                e.SendMessage(this, ComponentMessageType.GetWearLoc, replies);
                if (replies.Count > 0 && replies[0].messageType == ComponentMessageType.ReturnWearLoc)
                {
                    EquipEntityToPart((GUIBodyPart)replies[0].paramsList[0], e);
                }
            }
        }

        // Equips whatever we currently have in our active hand
        private void EquipEntityInHand()
        {
            if (!Owner.HasComponent(SS3D_shared.GO.ComponentFamily.Hands))
            {
                return; //TODO REAL ERROR MESSAGE OR SOME FUCK SHIT
            }
            List<ComponentReplyMessage> reps = new List<ComponentReplyMessage>();
            //Get the item in the hand
            Owner.SendMessage(this, ComponentMessageType.GetActiveHandItem, reps);
            if (reps.Count > 0 && reps[0].messageType == ComponentMessageType.ReturnActiveHandItem && CanEquip((Entity)reps[0].paramsList[0]))
            {
                //Remove from hand
                Owner.SendMessage(this, ComponentMessageType.DropItemInCurrentHand, null);
                //Equip
                EquipEntity((Entity)reps[0].paramsList[0]);
            }
        }

        // Unequips the entity from Part part
        private void UnEquipEntity(GUIBodyPart part)
        {
            if (!IsEmpty(part)) //If the part is not empty
            {
                equippedEntities[part].SendMessage(this, SS3D_shared.GO.ComponentMessageType.ItemUnEquipped, null);
                Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, EquipmentComponentNetMessage.ItemUnEquipped, part, equippedEntities[part].Uid);
                equippedEntities.Remove(part);
            }
        }

        private void UnEquipEntityToHand(Entity e)
        {
            UnEquipEntity(e);
            HumanHandsComponent hh = (HumanHandsComponent)Owner.GetComponent(ComponentFamily.Hands);
            Owner.SendMessage(this, ComponentMessageType.PickUpItem, null, e);
        }

        // Unequips entity e 
        private void UnEquipEntity(Entity e)
        {
            GUIBodyPart key;
            foreach (var kvp in equippedEntities)
            {
                if(kvp.Value == e)
                {
                    key = kvp.Key;
                    UnEquipEntity(key);
                    break;
                }
            }
        }
        
        // Unequips all entites
        private void UnEquipAllEntities()
        {
            foreach (Entity e in equippedEntities.Values)
            {
                UnEquipEntity(e);
            }
        }

        private bool IsItem(Entity e)
        {
            if (e.HasComponent(SS3D_shared.GO.ComponentFamily.Item)) //We can only equip items derp
                return true;
            return false;
        }

        private Entity GetEntity(GUIBodyPart part)
        {
            if (!IsEmpty(part))
                return equippedEntities[part];
            else
                return null;
        }

        private bool IsEmpty(GUIBodyPart part)
        {
            if (equippedEntities.ContainsKey(part))
                return false;
            return true;
        }

        private bool CanEquip(Entity e)
        {
            if(!e.HasComponent(ComponentFamily.Equippable))
                return false;

            List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
            e.SendMessage(this, ComponentMessageType.GetWearLoc, replies);
            if (replies.Count > 0 && replies[0].messageType == ComponentMessageType.ReturnWearLoc)
            {
                if (IsItem(e) && IsEmpty((GUIBodyPart)replies[0].paramsList[0]) && e != null && activeSlots.Contains((GUIBodyPart)replies[0].paramsList[0]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
