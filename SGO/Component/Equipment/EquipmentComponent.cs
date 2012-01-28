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
        protected Dictionary<EquipmentSlot, Entity> equippedEntities = new Dictionary<EquipmentSlot,Entity>();
        protected List<EquipmentSlot> activeSlots = new List<EquipmentSlot>();

        public EquipmentComponent()
        {
            family = ComponentFamily.Equipment;
        }

        public override void RecieveMessage(object sender, SS3D_shared.GO.ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case ComponentMessageType.EquipItemToPart: //Equip an entity straight up.
                    EquipEntityToPart((EquipmentSlot)list[0], (Entity)list[1]);
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
                case ComponentMessageType.UnEquipItemToSpecifiedHand: //remove an entity from a slot and put it in the current hand slot.
                    if (!Owner.HasComponent(SS3D_shared.GO.ComponentFamily.Hands))
                        return; //TODO REAL ERROR MESSAGE OR SOME FUCK SHIT
                    UnEquipEntityToHand((Entity)list[0], (Hand)list[1]);
                    break;
            }
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            if (message.componentFamily == ComponentFamily.Equipment)
            {
                ComponentMessageType type = (ComponentMessageType)message.messageParameters[0];
                List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
                switch(type) //Why does this send messages to itself THIS IS DUMB AND WILL BREAK THINGS. BZZZ
                {
                    case ComponentMessageType.EquipItem:
                        EquipEntity(EntityManager.Singleton.GetEntity((int)message.messageParameters[1]));
                        break;
                    case ComponentMessageType.EquipItemInHand:
                        EquipEntityInHand();
                        break;
                    case ComponentMessageType.EquipItemToPart:
                        EquipEntityToPart((EquipmentSlot)message.messageParameters[1], EntityManager.Singleton.GetEntity((int)message.messageParameters[2]));
                        break;
                    case ComponentMessageType.UnEquipItemToFloor:
                        UnEquipEntity(EntityManager.Singleton.GetEntity((int)message.messageParameters[1]));
                        break;
                    case ComponentMessageType.UnEquipItemToHand:
                        if (!Owner.HasComponent(SS3D_shared.GO.ComponentFamily.Hands))
                            return; //TODO REAL ERROR MESSAGE OR SOME FUCK SHIT
                        UnEquipEntityToHand(EntityManager.Singleton.GetEntity((int)message.messageParameters[1]));
                        break;
                    case ComponentMessageType.UnEquipItemToSpecifiedHand:
                        if (!Owner.HasComponent(SS3D_shared.GO.ComponentFamily.Hands))
                            return; //TODO REAL ERROR MESSAGE OR SOME FUCK SHIT
                        UnEquipEntityToHand(EntityManager.Singleton.GetEntity((int)message.messageParameters[1]), (Hand)message.messageParameters[2]);
                        break;
                }
            }
        }

        public override void HandleInstantiationMessage(Lidgren.Network.NetConnection netConnection)
        {
            foreach (EquipmentSlot p in equippedEntities.Keys)
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
        private void EquipEntityToPart(EquipmentSlot part, Entity e)
        {
            if (equippedEntities.ContainsValue(e)) //Its already equipped? Unequip first. This shouldnt happen.
                UnEquipEntity(e);

            if (CanEquip(e)) //If the part is empty, the part exists on this mob, and the entity specified is not null
            {
                if (Owner.HasComponent(ComponentFamily.Hands))
                    Owner.SendMessage(this, ComponentMessageType.DropEntityInHand, null, e);

                equippedEntities.Add(part, e);
                e.SendMessage(this, SS3D_shared.GO.ComponentMessageType.ItemEquipped, null, Owner);
                Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, EquipmentComponentNetMessage.ItemEquipped, part, e.Uid);
            }
        }

        // Equips Entity e and automatically finds the appropriate part
        private void EquipEntity(Entity e)
        {
            if(equippedEntities.ContainsValue(e)) //Its already equipped? Unequip first. This shouldnt happen.
                UnEquipEntity(e);

            if (CanEquip(e))
            {
                List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
                e.SendMessage(this, ComponentMessageType.GetWearLoc, replies);
                if (replies.Count > 0 && replies[0].messageType == ComponentMessageType.ReturnWearLoc)
                {
                    if (Owner.HasComponent(ComponentFamily.Hands))
                        Owner.SendMessage(this, ComponentMessageType.DropEntityInHand, null, e);

                    EquipEntityToPart((EquipmentSlot)replies[0].paramsList[0], e);
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
        private void UnEquipEntity(EquipmentSlot part)
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
            //HumanHandsComponent hh = (HumanHandsComponent)Owner.GetComponent(ComponentFamily.Hands);
            Owner.SendMessage(this, ComponentMessageType.PickUpItem, null, e);
        }

        private void UnEquipEntityToHand(Entity e, Hand h)
        {
            List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
            HumanHandsComponent hands = (HumanHandsComponent)Owner.GetComponent(ComponentFamily.Hands);
            Owner.SendMessage(this, ComponentMessageType.IsHandEmpty, replies, h);
            if (replies.Exists(x => x.messageType == ComponentMessageType.IsHandEmptyReply && (bool)x.paramsList[0]))
            {
                UnEquipEntity(e);
                Owner.SendMessage(this, ComponentMessageType.PickUpItemToHand, null, e, h);
            }
        }

        // Unequips entity e 
        private void UnEquipEntity(Entity e)
        {
            EquipmentSlot key;
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

        private Entity GetEntity(EquipmentSlot part)
        {
            if (!IsEmpty(part))
                return equippedEntities[part];
            else
                return null;
        }

        private bool IsEmpty(EquipmentSlot part)
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
                if (IsItem(e) && IsEmpty((EquipmentSlot)replies[0].paramsList[0]) && e != null && activeSlots.Contains((EquipmentSlot)replies[0].paramsList[0]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
