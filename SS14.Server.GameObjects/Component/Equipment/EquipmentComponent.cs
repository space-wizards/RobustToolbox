using Lidgren.Network;
using SS14.Server.GameObjects.Events;
using SS14.Server.GameObjects.Item.ItemCapability;
using SS14.Server.Interfaces.GameObject;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Equipment;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Server.GameObjects
{
    public class EquipmentComponent : Component, IEquipmentComponent
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
            }

            return reply;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            // TODO change these things to flow to the server via a system instead of via this component
            if (message.ComponentFamily == ComponentFamily.Equipment)
            {
                var type = (ComponentMessageType) message.MessageParameters[0];
                switch (type) //Why does this send messages to itself THIS IS DUMB AND WILL BREAK THINGS. BZZZ
                {
                    case ComponentMessageType.EquipItem:
                        RaiseEquipItem(Owner.EntityManager.GetEntity((int)message.MessageParameters[1]));
                        break;
                    case ComponentMessageType.EquipItemInHand:
                        RaiseEquipItemInHand();
                        break;
                    case ComponentMessageType.UnEquipItemToFloor:
                        RaiseUnEquipItemToFloor(Owner.EntityManager.GetEntity((int)message.MessageParameters[1]));
                        break;
                    case ComponentMessageType.UnEquipItemToHand:
                        RaiseUnEquipItemToHand(Owner.EntityManager.GetEntity((int)message.MessageParameters[1]));
                        break;
                    case ComponentMessageType.UnEquipItemToSpecifiedHand:
                        RaiseUnEquipItemToSpecifiedHand(Owner.EntityManager.GetEntity((int)message.MessageParameters[1]), (InventoryLocation)message.MessageParameters[2]);
                        break;
                }
            }
        }

        public void RaiseEquipItem(Entity item)
        {
            Owner.EntityManager.RaiseEvent(this, new InventoryEquipItemEventArgs
            {
                Actor = Owner,
                Item = item
            });
        }

        public void RaiseEquipItemInHand()
        {
            Owner.EntityManager.RaiseEvent(this, new InventoryEquipItemInHandEventArgs
            {
                Actor = Owner
            });
        }

        public void RaiseUnEquipItemToFloor(Entity item)
        {
            Owner.EntityManager.RaiseEvent(this, new InventoryUnEquipItemToFloorEventArgs
            {
                Actor = Owner,
                Item = item
            });
        }

        public void RaiseUnEquipItemToHand(Entity item)
        {
            Owner.EntityManager.RaiseEvent(this, new InventoryUnEquipItemToHandEventArgs
            {
                Actor = Owner,
                Item = item
            });
        }

        public void RaiseUnEquipItemToSpecifiedHand(Entity item, InventoryLocation hand)
        {
            Owner.EntityManager.RaiseEvent(this, new InventoryUnEquipItemToSpecifiedHandEventArgs
            {
                Actor = Owner,
                Item = item,
                Hand = hand
            });
        }

        // Equips Entity e to Part part
        public void EquipEntityToPart(EquipmentSlot part, Entity e)
        {
            if (equippedEntities.ContainsValue(e)) //Its already equipped? Unequip first. This shouldnt happen.
                UnEquipEntity(e);

            if (CanEquip(e)) //If the part is empty, the part exists on this mob, and the entity specified is not null
            {
                RemoveFromOtherComps(e);

                equippedEntities.Add(part, e);
                e.SendMessage(this, ComponentMessageType.ItemEquipped, Owner);
                //Owner.SendDirectedComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered, null,
                //                                          EquipmentComponentNetMessage.ItemEquipped, part, e.Uid);
            }
        }

        // Equips Entity e and automatically finds the appropriate part
        public void EquipEntity(Entity e)
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
        public void EquipEntityInHand()
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
        public void UnEquipEntity(EquipmentSlot part)
        {
            if (!IsEmpty(part)) //If the part is not empty
            {
                equippedEntities[part].SendMessage(this, ComponentMessageType.ItemUnEquipped);
                //Owner.SendDirectedComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered, null,
                //                                          EquipmentComponentNetMessage.ItemUnEquipped, part,
                //                                          equippedEntities[part].Uid);
                equippedEntities.Remove(part);
            }
        }

        public void UnEquipEntityToHand(Entity e)
        {
            UnEquipEntity(e);
            //HumanHandsComponent hh = (HumanHandsComponent)Owner.GetComponent(ComponentFamily.Hands);
            Owner.SendMessage(this, ComponentMessageType.PickUpItem, e);
        }

        public void UnEquipEntityToHand(Entity e, InventoryLocation h)
        {
            var hands = (HumanHandsComponent) Owner.GetComponent(ComponentFamily.Hands);
            ComponentReplyMessage reply = Owner.SendMessage(this, ComponentFamily.Hands,
                                                            ComponentMessageType.IsHandEmpty, h);
            if (reply.MessageType == ComponentMessageType.IsHandEmptyReply && (bool) reply.ParamsList[0])
            {
                UnEquipEntity(e);
                Owner.SendMessage(this, ComponentMessageType.PickUpItemToHand, e, h);
            }
        }

        public bool IsEquipped(Entity e)
        {
            return equippedEntities.ContainsValue(e);
        }

        // Unequips entity e
        public void UnEquipEntity(Entity e)
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

        private bool IsItem(Entity e)
        {
            if (e.HasComponent(ComponentFamily.Item)) //We can only equip items derp
                return true;
            return false;
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
                holder = ((BasicItemComponent) entity.GetComponent(ComponentFamily.Item)).CurrentHolder;
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
            foreach (Entity ent in equippedEntities.Values)
            {
                ent.SendMessage(this, ComponentMessageType.ItemGetAllCapabilities, replies);
            }
            foreach (ComponentReplyMessage reply in replies)
            {
                caps.AddRange((ItemCapability[]) reply.ParamsList[0]);
            }
            return caps;
        }

        public bool HasInternals()
        {
            return false;
        }

        public bool RemoveEntity(Entity user, Entity toRemove)
        {
            if (equippedEntities.Any(x => x.Value == toRemove))
            {
                EquippableComponent eqCompo = toRemove.GetComponent<EquippableComponent>(ComponentFamily.Equippable);

                if (eqCompo != null)
                    eqCompo.currentWearer = null;

                equippedEntities.Remove(equippedEntities.First(x => x.Value == toRemove).Key);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool CanAddEntity(Entity user, Entity toAdd)
        {
            if (equippedEntities.Any(x => x.Value == toAdd) || !toAdd.HasComponent(ComponentFamily.Equippable))
            {
                return false;
            }
            var eqCompo = toAdd.GetComponent<EquippableComponent>(ComponentFamily.Equippable);
            if (!activeSlots.Contains(eqCompo.wearloc) || equippedEntities.ContainsKey(eqCompo.wearloc))
            {
                return false;
            }
            return true;
        }

        public bool AddEntity(Entity user, Entity toAdd)
        {
            if (equippedEntities.Any(x => x.Value == toAdd))
                return false;
            else
            {
                EquippableComponent eqCompo = toAdd.GetComponent<EquippableComponent>(ComponentFamily.Equippable);
                if (eqCompo != null)
                {
                    if (activeSlots.Contains(eqCompo.wearloc) && !equippedEntities.ContainsKey(eqCompo.wearloc))
                    {
                        equippedEntities.Add(eqCompo.wearloc, toAdd);
                        eqCompo.currentWearer = this.Owner;

                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
        }

        public override ComponentState GetComponentState()
        {
            Dictionary<EquipmentSlot, int> equipped = equippedEntities.Select(x => new KeyValuePair<EquipmentSlot, int>(x.Key, x.Value.Uid)).ToDictionary(key => key.Key, va => va.Value);
            return new EquipmentComponentState(equipped, activeSlots);
        }
    }
}
