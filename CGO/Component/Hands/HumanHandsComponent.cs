using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.GOC;
using ClientInterfaces.UserInterface;
using SS13.IoC;
using SS13_Shared.GO;
using SS13_Shared;
using ClientInterfaces;

namespace CGO.Component.Hands
{
    public class HumanHandsComponent : GameObjectComponent
    {
        private Dictionary<Hand, IEntity> handslots;
        public Dictionary<Hand, IEntity> HandSlots { get { return handslots; } private set { handslots = value; } }
        public Hand currentHand { get; private set; }

        public HumanHandsComponent()
            : base()
        {
            family = ComponentFamily.Hands;
            handslots = new Dictionary<Hand, IEntity>();
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
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
                    if (handslots.Keys.Contains(usedHand))
                        handslots[usedHand] = item;          
                    else
                        handslots.Add(usedHand, item);
                    break;
                case(ComponentMessageType.HandsDroppedItem):
                    entityUid = (int)message.MessageParameters[1];
                    usedHand = (Hand)message.MessageParameters[2];
                    item = EntityManager.Singleton.GetEntity(entityUid);
                    //item.SendMessage(this, ComponentMessageType.Dropped, null);
                    handslots.Remove(usedHand);
                    break;
                case(ComponentMessageType.HandsPickedUpItem):
                    entityUid = (int)message.MessageParameters[1];
                    usedHand = (Hand)message.MessageParameters[2];
                    item = EntityManager.Singleton.GetEntity(entityUid);
                    //item.SendMessage(this, ComponentMessageType.PickedUp, null, usedHand);
                    handslots.Add(usedHand, item);
                    break;
                case ComponentMessageType.ActiveHandChanged:
                    SwitchHandTo((Hand)message.MessageParameters[1]);
                    break;
            }

            IoCManager.Resolve<IUserInterfaceManager>().ComponentUpdate(GuiComponentType.ComboGui, ComboGuiMessage.UpdateHands);
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
            if (handslots.ContainsKey(hand)) return false;
            else return true;
        }

        public void SendDropFromHand(Hand hand)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, ComponentMessageType.DropItemInHand, hand);
        }

        private void SwitchHandTo(Hand hand)
        {
            currentHand = hand;
        }
    }
}
