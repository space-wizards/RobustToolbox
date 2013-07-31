using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.GOC;
using ClientInterfaces.UserInterface;
using Lidgren.Network;
using SS13.IoC;
using SS13_Shared.GO;
using SS13_Shared;

namespace CGO
{
    public class HumanHandsComponent : GameObjectComponent
    {
        public Dictionary<Hand, IEntity> HandSlots { get; private set; }
        public Hand CurrentHand { get; private set; }

        public HumanHandsComponent():base()
        {
            HandSlots = new Dictionary<Hand, IEntity>();
            Family = ComponentFamily.Hands;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
            var type = (ComponentMessageType)message.MessageParameters[0];
            int entityUid;
            Hand usedHand;
            IEntity item;

            switch(type)
            {
                case(ComponentMessageType.EntityChanged): //This is not sent atm. Commented out serverside for later use.
                    entityUid = (int)message.MessageParameters[1];
                    usedHand = (Hand)message.MessageParameters[2];
                    item = EntityManager.Singleton.GetEntity(entityUid);
                    if (HandSlots.Keys.Contains(usedHand))
                        HandSlots[usedHand] = item;          
                    else
                        HandSlots.Add(usedHand, item);
                    break;
                case(ComponentMessageType.HandsDroppedItem):
                    //entityUid = (int)message.MessageParameters[1];
                    usedHand = (Hand)message.MessageParameters[2];
                    //item = EntityManager.Singleton.GetEntity(entityUid);
                    //item.SendMessage(this, ComponentMessageType.Dropped, null);
                    HandSlots.Remove(usedHand);
                    break;
                case(ComponentMessageType.HandsPickedUpItem):
                    entityUid = (int)message.MessageParameters[1];
                    usedHand = (Hand)message.MessageParameters[2];
                    item = EntityManager.Singleton.GetEntity(entityUid);
                    //item.SendMessage(this, ComponentMessageType.PickedUp, null, usedHand);
                    HandSlots.Add(usedHand, item);
                    break;
                case ComponentMessageType.ActiveHandChanged:
                    SwitchHandTo((Hand)message.MessageParameters[1]);
                    break;
            }

            IoCManager.Resolve<IUserInterfaceManager>().ComponentUpdate(GuiComponentType.HandsUi);
        }

        public void SendSwitchHands(Hand hand)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, ComponentMessageType.ActiveHandChanged, hand);
        }

        public void SendDropEntity(IEntity ent)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, ComponentMessageType.DropEntityInHand, ent.Uid);
        }

        public bool IsHandEmpty(Hand hand)
        {
            return !HandSlots.ContainsKey(hand);
        }

        public void SendDropFromHand(Hand hand)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, ComponentMessageType.DropItemInHand, hand);
        }

        private void SwitchHandTo(Hand hand)
        {
            CurrentHand = hand;
        }
    }
}
