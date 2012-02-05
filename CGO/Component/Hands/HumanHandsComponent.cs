using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared.GO;
using SS13_Shared;
using System.Drawing;
using ClientServices;
using ClientInterfaces;

namespace CGO
{
    public class HumanHandsComponent : GameObjectComponent
    {
        private Dictionary<Hand, Entity> handslots;
        public Dictionary<Hand, Entity> HandSlots { get { return handslots; } private set { handslots = value; } }
        public Hand currentHand { get; private set; }

        public HumanHandsComponent()
            : base()
        {
            family = ComponentFamily.Hands;
            handslots = new Dictionary<Hand, Entity>();
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            ComponentMessageType type = (ComponentMessageType)message.messageParameters[0];
            int entityUID;
            Hand usedHand;
            Entity item;

            switch(type)
            {
                case(ComponentMessageType.EntityChanged): //This is not sent atm. Commented out serverside for later use.
                    entityUID = (int)message.messageParameters[1];
                    usedHand = (Hand)message.messageParameters[2];
                    item = EntityManager.Singleton.GetEntity(entityUID);
                    if (handslots.Keys.Contains(usedHand))
                        handslots[usedHand] = item;          
                    else
                        handslots.Add(usedHand, item);
                    break;
                case(ComponentMessageType.HandsDroppedItem):
                    entityUID = (int)message.messageParameters[1];
                    usedHand = (Hand)message.messageParameters[2];
                    item = EntityManager.Singleton.GetEntity(entityUID);
                    //item.SendMessage(this, ComponentMessageType.Dropped, null);
                    handslots.Remove(usedHand);
                    break;
                case(ComponentMessageType.HandsPickedUpItem):
                    entityUID = (int)message.messageParameters[1];
                    usedHand = (Hand)message.messageParameters[2];
                    item = EntityManager.Singleton.GetEntity(entityUID);
                    //item.SendMessage(this, ComponentMessageType.PickedUp, null, usedHand);
                    handslots.Add(usedHand, item);
                    break;
                case ComponentMessageType.ActiveHandChanged:
                    SwitchHandTo((Hand)message.messageParameters[1]);
                    break;
            }

            ServiceManager.Singleton.GetUiManager().ComponentUpdate(GuiComponentType.ComboGUI, ComboGuiMessage.UpdateHands);
        }

        public void SendSwitchHands(Hand hand)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, ComponentMessageType.ActiveHandChanged, hand);
        }

        public void SendDropEntity(Entity ent)
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
