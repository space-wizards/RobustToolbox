using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;
using SS3D_shared;
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
                case(ComponentMessageType.Dropped):
                    entityUID = (int)message.messageParameters[1];
                    usedHand = (Hand)message.messageParameters[2];
                    item = EntityManager.Singleton.GetEntity(entityUID);
                    handslots.Remove(usedHand);
                    break;
                case(ComponentMessageType.PickedUp):
                    entityUID = (int)message.messageParameters[1];
                    usedHand = (Hand)message.messageParameters[2];
                    item = EntityManager.Singleton.GetEntity(entityUID);
                    handslots.Add(usedHand, item);
                    break;
                case ComponentMessageType.ActiveHandChanged:
                    SwitchHandTo((Hand)message.messageParameters[1]);
                    break;
            }

            IUserInterfaceManager UIManager = (IUserInterfaceManager)ServiceManager.Singleton.GetService(ClientServiceType.UiManager);

            if (UIManager == null)
                throw new NullReferenceException("No UI Manager Service found.");

            UIManager.ComponentUpdate(GuiComponentType.AppendagesComponent);
        }

        public void SendSwitchHands(Hand hand)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, ComponentMessageType.ActiveHandChanged, hand);
        }

        private void SwitchHandTo(Hand hand)
        {
            currentHand = hand;
        }
    }
}
