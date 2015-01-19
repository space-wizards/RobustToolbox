using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.UserInterface;
using GameObject;
using Lidgren.Network;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Hands;

namespace CGO
{
    public class HumanHandsComponent : Component
    {
        public Dictionary<Hand, Entity> HandSlots { get; private set; }
        public Hand CurrentHand { get; private set; }

        public HumanHandsComponent()
        {
            HandSlots = new Dictionary<Hand, Entity>();
            Family = ComponentFamily.Hands;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
            var type = (ComponentMessageType) message.MessageParameters[0];
            int entityUid;
            Hand usedHand;
            Entity item;

            switch (type)
            {
                case (ComponentMessageType.EntityChanged):
                    //This is not sent atm. Commented out serverside for later use.
                    entityUid = (int) message.MessageParameters[1];
                    usedHand = (Hand) message.MessageParameters[2];
                    item = Owner.EntityManager.GetEntity(entityUid);
                    if (HandSlots.Keys.Contains(usedHand))
                        HandSlots[usedHand] = item;
                    else
                        HandSlots.Add(usedHand, item);
                    break;
                case (ComponentMessageType.HandsDroppedItem):
                    //entityUid = (int)message.MessageParameters[1];
                    usedHand = (Hand) message.MessageParameters[2];
                    //item = EntityManager.Singleton.GetEntity(entityUid);
                    //item.SendMessage(this, ComponentMessageType.Dropped, null);
                    HandSlots.Remove(usedHand);
                    break;
                case (ComponentMessageType.HandsPickedUpItem):
                    entityUid = (int) message.MessageParameters[1];
                    usedHand = (Hand) message.MessageParameters[2];
                    item = Owner.EntityManager.GetEntity(entityUid);
                    //item.SendMessage(this, ComponentMessageType.PickedUp, null, usedHand);
                    HandSlots.Add(usedHand, item);
                    break;
                case ComponentMessageType.ActiveHandChanged:
                    SwitchHandTo((Hand) message.MessageParameters[1]);
                    break;
            }

            IoCManager.Resolve<IUserInterfaceManager>().ComponentUpdate(GuiComponentType.HandsUi);
        }

        public override System.Type StateType
        {
            get
            {
                return typeof (HandsComponentState);
            }
        }

        public void SendSwitchHands(Hand hand)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered,
                                              ComponentMessageType.ActiveHandChanged, hand);
        }

        public void SendDropEntity(Entity ent)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered,
                                              ComponentMessageType.DropEntityInHand, ent.Uid);
        }

        public bool IsHandEmpty(Hand hand)
        {
            return !HandSlots.ContainsKey(hand);
        }

        public void SendDropFromHand(Hand hand)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered,
                                              ComponentMessageType.DropItemInHand, hand);
        }

        private void SwitchHandTo(Hand hand)
        {
            CurrentHand = hand;
        }

        public override void HandleComponentState(dynamic state)
        {
            SetNewState(state);
        }

        private void SetNewState(HandsComponentState state)
        {
            bool changed = false;
            var currentHand = Hand.None;
            if(state.ActiveHand == InventoryLocation.HandLeft) currentHand = Hand.Left;
            if(state.ActiveHand == InventoryLocation.HandRight) currentHand = Hand.Right;
            if(CurrentHand != currentHand)
            {
                changed = true;
                CurrentHand = currentHand;
            }
            foreach(var handSlot in state.Slots.Keys)
            {
                var hand = inventoryLocationToHand(handSlot);
                if (!HandSlots.ContainsKey(hand))
                {
                    HandSlots.Add(hand, null);
                    changed = true;
                }
                var existingSlotUid = HandSlots[hand] == null ? null : (int?)HandSlots[hand].Uid;
                var newSlotUid = state.Slots[handSlot];
                if(existingSlotUid == null)
                {
                    if (newSlotUid != null)
                    {
                        HandSlots[hand] = Owner.EntityManager.GetEntity((int)newSlotUid);
                        changed = true;
                    }
                }
                else
                {
                    if(newSlotUid != existingSlotUid)
                    {
                        if (newSlotUid != null)
                            HandSlots[hand] = Owner.EntityManager.GetEntity((int)newSlotUid);
                        else
                            HandSlots[hand] = null;

                        changed = true;
                    }
                }
            }
            if(changed)
            {
                IoCManager.Resolve<IUserInterfaceManager>().ComponentUpdate(GuiComponentType.HandsUi);
            }
        }

        private Hand inventoryLocationToHand(InventoryLocation location)
        {
            if(location == InventoryLocation.HandLeft)
                return Hand.Left;
            if(location == InventoryLocation.HandRight)
                return Hand.Right;
            return Hand.None;
        }
    }
}