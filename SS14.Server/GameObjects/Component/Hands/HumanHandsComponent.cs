using Lidgren.Network;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Hands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SS14.Server.GameObjects
{
    public class HumanHandsComponent : Component, IInventoryContainer
    {
        public readonly Dictionary<InventoryLocation, Entity> Handslots;
        public InventoryLocation CurrentHand = InventoryLocation.HandLeft;

        public HumanHandsComponent()
        {
            Family = ComponentFamily.Hands;
            Handslots = new Dictionary<InventoryLocation, Entity>();
        }

        public override void HandleExtendedParameters(XElement extendedParameters)
        {
            Handslots.Clear();

            foreach (XElement param in extendedParameters.Descendants("handSlot"))
            {
                if (param.Attribute("slot") != null)
                    Handslots.Add((InventoryLocation)Enum.Parse(typeof(InventoryLocation), param.Attribute("slot").Value), null);
            }
        }

        /*
        /// <summary>
        /// Recieve a component message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="type"></param>
        /// <param name="replies"></param>
        /// <param name="list"></param>
        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.DisassociateEntity:
                    var entDrop = (Entity) list[0];
                    Drop(entDrop);
                    break;
                case ComponentMessageType.ActiveHandChanged:
                    SwitchHandsTo((Hand) list[0]);
                    break;
                case ComponentMessageType.IsCurrentHandEmpty:
                    reply = new ComponentReplyMessage(ComponentMessageType.IsCurrentHandEmpty, IsEmpty(CurrentHand));
                    break;
                case ComponentMessageType.IsHandEmpty:
                    reply = new ComponentReplyMessage(ComponentMessageType.IsHandEmptyReply, IsEmpty((Hand) list[0]));
                    break;
                case ComponentMessageType.PickUpItem:
                    Pickup((Entity) list[0]);
                    break;
                case ComponentMessageType.PickUpItemToHand:
                    Pickup((Entity) list[0], (Hand) list[1]);
                    break;
                case ComponentMessageType.DropItemInCurrentHand:
                    Drop(CurrentHand);
                    break;
                case ComponentMessageType.DropItemInHand:
                    var hand = (Hand) list[0];
                    Drop(hand);
                    break;
                case ComponentMessageType.DropEntityInHand:
                    var ent = (Entity) list[0];
                    Drop(ent);
                    break;
                case ComponentMessageType.BoundKeyChange:
                    if ((BoundKeyFunctions) list[0] == BoundKeyFunctions.Drop &&
                        (BoundKeyState) list[1] == BoundKeyState.Up)
                        Drop();
                    if ((BoundKeyFunctions) list[0] == BoundKeyFunctions.SwitchHands &&
                        (BoundKeyState) list[1] == BoundKeyState.Up)
                    {
                        SwitchHands();
                    }
                    if ((BoundKeyFunctions) list[0] == BoundKeyFunctions.ActivateItemInHand &&
                        (BoundKeyState) list[1] == BoundKeyState.Up)
                        ActivateItemInHand();

                    break;
                case ComponentMessageType.GetActiveHandItem:
                    if (!IsEmpty(CurrentHand))
                        reply = new ComponentReplyMessage(ComponentMessageType.ReturnActiveHandItem,
                                                          Handslots[CurrentHand]);
                    break;
                case ComponentMessageType.Die:
                    DropAll();
                    break;
            }

            return reply;
        }*/

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            if (message.ComponentFamily == ComponentFamily.Hands)
            {
                var type = (ComponentMessageType) message.MessageParameters[0];
                var replies = new List<ComponentReplyMessage>();
                switch (type)
                {
                    case ComponentMessageType.ActiveHandChanged:
                        var hand = (InventoryLocation)message.MessageParameters[1];
                        SwitchHandsTo(hand);
                        break;
                    case ComponentMessageType.DropEntityInHand:
                        Drop(Owner.EntityManager.GetEntity((int) message.MessageParameters[1]));
                        break;
                    case ComponentMessageType.DropItemInHand:
                        var dhand = (InventoryLocation)message.MessageParameters[1];
                        Drop(dhand);
                        break;
                }
            }
        }


        /// <summary>
        /// Change the currently selected hand
        /// </summary>
        public void SwitchHands()
        {
            if (CurrentHand == InventoryLocation.HandLeft)
                SwitchHandsTo(InventoryLocation.HandRight);
            else
                SwitchHandsTo(InventoryLocation.HandLeft);
        }

        private void SwitchHandsTo(InventoryLocation hand)
        {
            CurrentHand = hand;
            Owner.SendDirectedComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered, null,
                                                      ComponentMessageType.ActiveHandChanged, hand);
        }

        /// <summary>
        /// Get the entity in the specified hand
        /// </summary>
        /// <param name="hand"></param>
        /// <returns></returns>
        public Entity GetEntity(InventoryLocation hand)
        {
            if (!IsEmpty(hand))
                return Handslots[hand];
            else
                return null;
        }

        /// <summary>
        /// Get the entity in the specified hand
        /// </summary>
        /// <param name="hand"></param>
        /// <returns></returns>
        public InventoryLocation GetHand(Entity entity)
        {
            foreach(var kvp in Handslots)
            {
                if (kvp.Value == entity)
                    return kvp.Key;
            }
            return InventoryLocation.None;
        }

        /// <summary>
        /// Get the currently selected hand
        /// </summary>
        /// <returns></returns>
        private InventoryLocation GetCurrentHand()
        {
            return CurrentHand;
        }

        private void ActivateItemInHand()
        {
            InventoryLocation h = GetCurrentHand();
            if (!IsEmpty(h))
            {
                Entity e = GetEntity(h);
                if (e != null)
                {
                    e.SendMessage(this, ComponentFamily.Item, ComponentMessageType.Activate);
                }
            }
        }

        /// <summary>
        /// Set the entity in the specified hand
        /// </summary>
        /// <param name="hand"></param>
        /// <param name="entity"></param>
        private void SetEntity(InventoryLocation hand, Entity entity)
        {
            if (entity != null && IsEmpty(hand))
            {
                Handslots.Add(hand, entity);
                //Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, null, ComponentMessageType.EntityChanged, entity.Uid, hand); Maybe for later use?
            }
        }

        /// <summary>
        /// Put the specified entity in the currently selected hand
        /// </summary>
        /// <param name="entity"></param>
        private void Pickup(Entity entity)
        {
            if (entity != null && IsEmpty(CurrentHand))
            {
                RemoveFromOtherComps(entity);

                SetEntity(CurrentHand, entity);
                Owner.SendDirectedComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered, null,
                                                          ComponentMessageType.HandsPickedUpItem, entity.Uid,
                                                          CurrentHand);
                entity.SendMessage(this, ComponentMessageType.PickedUp, Owner, CurrentHand);
            }
        }

        /// <summary>
        /// Put the specified entity in the specified hand
        /// </summary>
        /// <param name="entity"></param>
        private void Pickup(Entity entity, InventoryLocation hand)
        {
            if (entity != null && IsEmpty(hand))
            {
                RemoveFromOtherComps(entity);

                SetEntity(hand, entity);
                Owner.SendDirectedComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered, null,
                                                          ComponentMessageType.HandsPickedUpItem, entity.Uid, hand);
                entity.SendMessage(this, ComponentMessageType.PickedUp, Owner, hand);
            }
        }

        /// <summary>
        /// Drop the item in the currently selected hand
        /// </summary>
        private void Drop()
        {
            Drop(CurrentHand);
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

        /// <summary>
        /// Drop an item from a hand.
        /// </summary>
        /// <param name="hand"></param>
        private void Drop(InventoryLocation hand)
        {
            if (!IsEmpty(hand))
            {
                GetEntity(hand).SendMessage(this, ComponentMessageType.Dropped);
                Owner.SendDirectedComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered, null,
                                                          ComponentMessageType.HandsDroppedItem, GetEntity(hand).Uid,
                                                          hand);
                Handslots.Remove(hand);
            }
        }

        /// <summary>
        /// Drop an entity.
        /// </summary>
        /// <param name="hand"></param>
        private void Drop(Entity ent)
        {
            if (Handslots.ContainsValue(ent))
            {
                InventoryLocation holding = Handslots.First(x => x.Value == ent).Key;
                Drop(holding);
            }
        }

        private void DropAll()
        {
            Drop(InventoryLocation.HandLeft);
            Drop(InventoryLocation.HandRight);
        }

        /// <summary>
        /// Check if the specified hand is empty
        /// </summary>
        /// <param name="hand"></param>
        /// <returns></returns>
        public bool IsEmpty(InventoryLocation hand)
        {
            if (Handslots.ContainsKey(hand) && Handslots[hand] != null)
                return false;
            return true;
        }

        public bool RemoveEntity(Entity actor, Entity toRemove, InventoryLocation location = InventoryLocation.Any)
        {
            if (Handslots.Any(x => x.Value == toRemove))
            {
                Handslots[Handslots.First(x => x.Value == toRemove).Key] = null;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool CanAddEntity(Entity actor, Entity toAdd, InventoryLocation location = InventoryLocation.Any)
        {
            if (Handslots.Any(x => x.Value == toAdd) || (location == InventoryLocation.Any && Handslots[CurrentHand] != null) || !Handslots.ContainsKey(location) || Handslots[location] != null)
            {
                return false;
            }
            return true;
        }

        public bool AddEntity(Entity actor, Entity toAdd, InventoryLocation location = InventoryLocation.Any)
        {
            if (Handslots.Any(x => x.Value == toAdd))
            {
                return false;
            }
            else
            {
                if (location == InventoryLocation.Any)
                {
                    if (Handslots[CurrentHand] != null)
                        return false;
                    else
                    {
                        Handslots[CurrentHand] = toAdd;
                    }
                }
                else
                {
                    if (Handslots[location] == null)
                    {
                        Handslots[location] = toAdd;
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public IEnumerable<Entity> GetEntitiesInInventory()
        {
            return Handslots.Values;
        }

        public override ComponentState GetComponentState()
        {
            //Oh man , what
            //Yes man, this!
            var entities = Handslots.Select(x => new KeyValuePair<InventoryLocation, int?>(x.Key, x.Value != null?(int?)x.Value.Uid:null)).ToDictionary(key => key.Key, va => va.Value);
            return new HandsComponentState(CurrentHand, entities);
        }

        public bool IsInHand(Entity e)
        {
            return Handslots.ContainsValue(e);
        }
    }
}
