using System.Collections.Generic;
using System.Linq;
using GameObject;
using Lidgren.Network;
using SGO.Item.ItemCapability;
using SS13_Shared;
using SS13_Shared.GO;

namespace SGO
{
    public class EquipmentComponent : Component
    {
        protected List<EquipmentSlot> activeSlots = new List<EquipmentSlot>();
        public Dictionary<EquipmentSlot, Entity> equippedEntities = new Dictionary<EquipmentSlot, Entity>();

        public EquipmentComponent()
        {
            Family = ComponentFamily.Equipment;
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.DisassociateEntity:
                    UnEquipEntity((Entity) list[0]);
                    break;
                case ComponentMessageType.EquipItemToPart: //Equip an entity straight up.
                    EquipEntityToPart((EquipmentSlot) list[0], (Entity) list[1]);
                    break;
                case ComponentMessageType.EquipItem:
                    EquipEntity((Entity) list[0]);
                    break;
                case ComponentMessageType.EquipItemInHand: //Move an entity from a hand to an equipment slot
                    EquipEntityInHand();
                    break;
                case ComponentMessageType.UnEquipItemToFloor: //remove an entity from a slot and drop it on the floor
                    UnEquipEntity((Entity) list[0]);
                    break;
                case ComponentMessageType.UnEquipItemToHand:
                    //remove an entity from a slot and put it in the current hand slot.
                    if (!Owner.HasComponent(ComponentFamily.Hands))
                        break; //TODO REAL ERROR MESSAGE OR SOME FUCK SHIT
                    UnEquipEntityToHand((Entity) list[0]);
                    break;
                case ComponentMessageType.UnEquipItemToSpecifiedHand:
                    //remove an entity from a slot and put it in the current hand slot.
                    if (!Owner.HasComponent(ComponentFamily.Hands))
                        break; //TODO REAL ERROR MESSAGE OR SOME FUCK SHIT
                    UnEquipEntityToHand((Entity) list[0], (Hand) list[1]);
                    break;
                case ComponentMessageType.GetHasInternals:
                    if (HasInternals())
                    {
                        reply = new ComponentReplyMessage(ComponentMessageType.GetHasInternals, true);
                    }
                    break;
            }

            return reply;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            if (message.ComponentFamily == ComponentFamily.Equipment)
            {
                var type = (ComponentMessageType) message.MessageParameters[0];
                var replies = new List<ComponentReplyMessage>();
                switch (type) //Why does this send messages to itself THIS IS DUMB AND WILL BREAK THINGS. BZZZ
                {
                    case ComponentMessageType.EquipItem:
                        EquipEntity(Owner.EntityManager.GetEntity((int) message.MessageParameters[1]));
                        break;
                    case ComponentMessageType.EquipItemInHand:
                        EquipEntityInHand();
                        break;
                    case ComponentMessageType.EquipItemToPart:
                        EquipEntityToPart((EquipmentSlot) message.MessageParameters[1],
                                          Owner.EntityManager.GetEntity((int)message.MessageParameters[2]));
                        break;
                    case ComponentMessageType.UnEquipItemToFloor:
                        UnEquipEntity((Entity)Owner.EntityManager.GetEntity((int)message.MessageParameters[1]));
                        break;
                    case ComponentMessageType.UnEquipItemToHand:
                        if (!Owner.HasComponent(ComponentFamily.Hands))
                            return; //TODO REAL ERROR MESSAGE OR SOME FUCK SHIT
                        UnEquipEntityToHand(Owner.EntityManager.GetEntity((int)message.MessageParameters[1]));
                        break;
                    case ComponentMessageType.UnEquipItemToSpecifiedHand:
                        if (!Owner.HasComponent(ComponentFamily.Hands))
                            return; //TODO REAL ERROR MESSAGE OR SOME FUCK SHIT
                        UnEquipEntityToHand(Owner.EntityManager.GetEntity((int)message.MessageParameters[1]),
                                            (Hand) message.MessageParameters[2]);
                        break;
                }
            }
        }

        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
            foreach (EquipmentSlot p in equippedEntities.Keys)
            {
                if (!IsEmpty(p))
                {
                    var e = equippedEntities[p];
                    e.SendMessage(this, ComponentMessageType.ItemEquipped, Owner);
                    Owner.SendDirectedComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered, netConnection,
                                                      EquipmentComponentNetMessage.ItemEquipped, p, e.Uid);
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
                RemoveFromOtherComps(e);

                equippedEntities.Add(part, e);
                e.SendMessage(this, ComponentMessageType.ItemEquipped, Owner);
                Owner.SendDirectedComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered, null,
                                                  EquipmentComponentNetMessage.ItemEquipped, part, e.Uid);
            }
        }

        // Equips Entity e and automatically finds the appropriate part
        private void EquipEntity(Entity e)
        {
            if (equippedEntities.ContainsValue(e)) //Its already equipped? Unequip first. This shouldnt happen.
                UnEquipEntity(e);

            if (CanEquip(e))
            {
                ComponentReplyMessage reply = e.SendMessage(this, ComponentFamily.Equippable,
                                                            ComponentMessageType.GetWearLoc);
                if (reply.MessageType == ComponentMessageType.ReturnWearLoc)
                {
                    RemoveFromOtherComps(e);
                    EquipEntityToPart((EquipmentSlot) reply.ParamsList[0], e);
                }
            }
        }

        // Equips whatever we currently have in our active hand
        private void EquipEntityInHand()
        {
            if (!Owner.HasComponent(ComponentFamily.Hands))
            {
                return; //TODO REAL ERROR MESSAGE OR SOME FUCK SHIT
            }
            //Get the item in the hand
            ComponentReplyMessage reply = Owner.SendMessage(this, ComponentFamily.Hands,
                                                            ComponentMessageType.GetActiveHandItem);
            if (reply.MessageType == ComponentMessageType.ReturnActiveHandItem && CanEquip((Entity) reply.ParamsList[0]))
            {
                RemoveFromOtherComps((Entity) reply.ParamsList[0]);
                //Equip
                EquipEntity((Entity) reply.ParamsList[0]);
            }
        }

        // Unequips the entity from Part part
        private void UnEquipEntity(EquipmentSlot part)
        {
            if (!IsEmpty(part)) //If the part is not empty
            {
                equippedEntities[part].SendMessage(this, ComponentMessageType.ItemUnEquipped);
                Owner.SendDirectedComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered, null,
                                                  EquipmentComponentNetMessage.ItemUnEquipped, part,
                                                  equippedEntities[part].Uid);
                equippedEntities.Remove(part);
            }
        }

        private void UnEquipEntityToHand(Entity e)
        {
            UnEquipEntity(e);
            //HumanHandsComponent hh = (HumanHandsComponent)Owner.GetComponent(ComponentFamily.Hands);
            Owner.SendMessage(this, ComponentMessageType.PickUpItem, e);
        }

        private void UnEquipEntityToHand(Entity e, Hand h)
        {
            var hands = (HumanHandsComponent) Owner.GetComponent(ComponentFamily.Hands);
            ComponentReplyMessage reply = Owner.SendMessage(this, ComponentFamily.Hands, ComponentMessageType.IsHandEmpty, h);
            if (reply.MessageType == ComponentMessageType.IsHandEmptyReply && (bool) reply.ParamsList[0])
            {
                UnEquipEntity(e);
                Owner.SendMessage(this, ComponentMessageType.PickUpItemToHand, e, h);
            }
        }

        // Unequips entity e 
        private void UnEquipEntity(Entity e)
        {
            EquipmentSlot key;
            foreach (var kvp in equippedEntities)
            {
                if (kvp.Value == e)
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
            if (e.HasComponent(ComponentFamily.Item)) //We can only equip items derp
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

        private void RemoveFromOtherComps(Entity entity)
        {
            Entity holder = null;
            if (entity.HasComponent(ComponentFamily.Item))
                holder = ((BasicItemComponent) entity.GetComponent(ComponentFamily.Item)).currentHolder;
            if (holder == null && entity.HasComponent(ComponentFamily.Equippable))
                holder = ((EquippableComponent) entity.GetComponent(ComponentFamily.Equippable)).currentWearer;
            if (holder != null) holder.SendMessage(this, ComponentMessageType.DisassociateEntity, entity);
            else Owner.SendMessage(this, ComponentMessageType.DisassociateEntity, entity);
        }

        private bool CanEquip(Entity e)
        {
            if (!e.HasComponent(ComponentFamily.Equippable))
                return false;

            ComponentReplyMessage reply = e.SendMessage(this, ComponentFamily.Equippable,
                                                        ComponentMessageType.GetWearLoc);
            if (reply.MessageType == ComponentMessageType.ReturnWearLoc)
            {
                if (IsItem(e) && IsEmpty((EquipmentSlot) reply.ParamsList[0]) && e != null &&
                    activeSlots.Contains((EquipmentSlot) reply.ParamsList[0]))
                {
                    return true;
                }
            }

            return false;
        }

        public List<ItemCapability> GetEquipmentCapabilities()
        {
            var caps = new List<ItemCapability>();
            var replies = new List<ComponentReplyMessage>();
            foreach(var ent in equippedEntities.Values)
            {
                ent.SendMessage(this, ComponentMessageType.ItemGetAllCapabilities, replies);
            }
            foreach(var reply in replies)
            {
                caps.AddRange((ItemCapability[])reply.ParamsList[0]);
            }
            return caps;
        }
        
        protected bool HasInternals()
        {
            var caps = GetEquipmentCapabilities();
            var cap = caps.FirstOrDefault(c => c.GetType() == typeof (BreatherCapability));
            if (cap != null)
                return true;
            return false;
        }
    }
}