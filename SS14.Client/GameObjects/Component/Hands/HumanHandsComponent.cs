using Lidgren.Network;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Hands;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.GameObjects
{
    [IoCTarget]
    public class HumanHandsComponent : ClientComponent
    {
        public override string Name => "HumanHands";
        public Dictionary<InventoryLocation, IEntity> HandSlots { get; private set; }
        public InventoryLocation CurrentHand { get; private set; }

        public HumanHandsComponent()
        {
            HandSlots = new Dictionary<InventoryLocation, IEntity>();
            Family = ComponentFamily.Hands;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
            var type = (ComponentMessageType)message.MessageParameters[0];
            int entityUid;
            InventoryLocation usedHand;
            IEntity item;

            switch (type)
            {
                case (ComponentMessageType.EntityChanged):
                    //This is not sent atm. Commented out serverside for later use.
                    entityUid = (int)message.MessageParameters[1];
                    usedHand = (InventoryLocation)message.MessageParameters[2];
                    item = Owner.EntityManager.GetEntity(entityUid);
                    if (HandSlots.Keys.Contains(usedHand))
                        HandSlots[usedHand] = item;
                    else
                        HandSlots.Add(usedHand, item);
                    break;
                case (ComponentMessageType.HandsDroppedItem):
                    //entityUid = (int)message.MessageParameters[1];
                    usedHand = (InventoryLocation)message.MessageParameters[2];
                    //item = EntityManager.Singleton.GetEntity(entityUid);
                    //item.SendMessage(this, ComponentMessageType.Dropped, null);
                    HandSlots.Remove(usedHand);
                    break;
                case (ComponentMessageType.HandsPickedUpItem):
                    entityUid = (int)message.MessageParameters[1];
                    usedHand = (InventoryLocation)message.MessageParameters[2];
                    item = Owner.EntityManager.GetEntity(entityUid);
                    //item.SendMessage(this, ComponentMessageType.PickedUp, null, usedHand);
                    HandSlots.Add(usedHand, item);
                    break;
                case ComponentMessageType.ActiveHandChanged:
                    SwitchHandTo((InventoryLocation)message.MessageParameters[1]);
                    break;
            }

            IoCManager.Resolve<IUserInterfaceManager>().ComponentUpdate(GuiComponentType.HandsUi);
        }

        public override System.Type StateType
        {
            get
            {
                return typeof(HandsComponentState);
            }
        }

        public void SendSwitchHands(InventoryLocation hand)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered,
                                              ComponentMessageType.ActiveHandChanged, hand);
        }

        public void SendDropEntity(IEntity ent)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered,
                                              ComponentMessageType.DropEntityInHand, ent.Uid);
        }

        public bool IsHandEmpty(InventoryLocation hand)
        {
            return !HandSlots.ContainsKey(hand);
        }

        public void SendDropFromHand(InventoryLocation hand)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered,
                                              ComponentMessageType.DropItemInHand, hand);
        }

        private void SwitchHandTo(InventoryLocation hand)
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
            var currentHand = state.ActiveHand;
            if (CurrentHand != currentHand)
            {
                changed = true;
                CurrentHand = currentHand;
            }
            foreach (var handSlot in state.Slots.Keys)
            {
                var hand = handSlot;
                if (!HandSlots.ContainsKey(hand))
                {
                    HandSlots.Add(hand, null);
                    changed = true;
                }
                var existingSlotUid = HandSlots[hand] == null ? null : (int?)HandSlots[hand].Uid;
                var newSlotUid = state.Slots[handSlot];
                if (existingSlotUid == null)
                {
                    if (newSlotUid != null)
                    {
                        HandSlots[hand] = Owner.EntityManager.GetEntity((int)newSlotUid);
                        changed = true;
                    }
                }
                else
                {
                    if (newSlotUid != existingSlotUid)
                    {
                        if (newSlotUid != null)
                            HandSlots[hand] = Owner.EntityManager.GetEntity((int)newSlotUid);
                        else
                            HandSlots[hand] = null;

                        changed = true;
                    }
                }
            }
            if (changed)
            {
                IoCManager.Resolve<IUserInterfaceManager>().ComponentUpdate(GuiComponentType.HandsUi);
            }
        }
    }
}
