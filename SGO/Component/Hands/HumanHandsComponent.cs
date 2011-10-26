using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared;
using SS3D_shared.GO;

namespace SGO
{
    public class HumanHandsComponent : GameObjectComponent
    {
        private Dictionary<Hand, Entity> handslots;
        private Hand currentHand = Hand.Left;

        public HumanHandsComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Hands;
            handslots = new Dictionary<Hand, Entity>();
        }

        /// <summary>
        /// Recieve a component message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="type"></param>
        /// <param name="replies"></param>
        /// <param name="list"></param>
        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case ComponentMessageType.ActiveHandChanged:
                    SwitchHandsTo((Hand)list[0]);
                    break;
                case ComponentMessageType.IsCurrentHandEmpty:
                    replies.Add(new ComponentReplyMessage(ComponentMessageType.IsCurrentHandEmpty, IsEmpty(currentHand)));
                    break;
                case ComponentMessageType.PickUpItem:
                    Pickup((Entity)list[0]);
                    break;
                case ComponentMessageType.DropItemInCurrentHand:
                    Drop(currentHand);
                    break;
                case ComponentMessageType.BoundKeyChange:
                    if ((BoundKeyFunctions)list[0] == BoundKeyFunctions.Drop)
                        Drop();
                    break;
                case ComponentMessageType.GetActiveHandItem:
                    if (!IsEmpty(currentHand))
                        replies.Add(new ComponentReplyMessage(ComponentMessageType.ReturnActiveHandItem, handslots[currentHand]));
                    break;
            }
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            if (message.componentFamily == ComponentFamily.Hands)
            {
                ComponentMessageType type = (ComponentMessageType)message.messageParameters[0];
                List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
                switch (type)
                {
                    case ComponentMessageType.ActiveHandChanged:
                        Owner.SendMessage(this, type, null, message.messageParameters[1]);
                        break;
                }
            }
        }


        /// <summary>
        /// Change the currently selected hand
        /// </summary>
        private void SwitchHands()
        {
            if (currentHand == Hand.Left)
                currentHand = Hand.Right;
            else
                currentHand = Hand.Left;
        }

        private void SwitchHandsTo(Hand hand)
        {
            currentHand = hand;
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, null, ComponentMessageType.ActiveHandChanged, hand);

        }

        /// <summary>
        /// Get the entity in the specified hand
        /// </summary>
        /// <param name="hand"></param>
        /// <returns></returns>
        private Entity GetEntity(Hand hand)
        {
            if (!IsEmpty(hand))
                return handslots[hand];
            else 
                return null;
        }

        /// <summary>
        /// Get the currently selected hand
        /// </summary>
        /// <returns></returns>
        private Hand GetCurrentHand()
        { return currentHand; }

        /// <summary>
        /// Set the entity in the specified hand
        /// </summary>
        /// <param name="hand"></param>
        /// <param name="entity"></param>
        private void SetEntity(Hand hand, Entity entity)
        {
            if (entity != null && IsEmpty(hand))
            {
                handslots.Add(hand, entity);
                //Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, null, ComponentMessageType.EntityChanged, entity.Uid, hand); Maybe for later use?
            }
        }

        /// <summary>
        /// Put the specified entity in the currently selected hand
        /// </summary>
        /// <param name="entity"></param>
        private void Pickup(Entity entity)
        {
            if (entity != null && IsEmpty(currentHand))
            {
                SetEntity(currentHand, entity);
                Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, null, ComponentMessageType.PickedUp, entity.Uid, currentHand);
                entity.SendMessage(this, ComponentMessageType.PickedUp, null, Owner);
            }
        }

        /// <summary>
        /// Drop the item in the currently selected hand
        /// </summary>
        private void Drop()
        {
            Drop(currentHand);    
        }

        /// <summary>
        /// Drop an item from a hand.
        /// </summary>
        /// <param name="hand"></param>
        private void Drop(Hand hand)
        {
            if (!IsEmpty(hand))
            {
                GetEntity(hand).SendMessage(this, ComponentMessageType.Dropped, null);
                Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, null, ComponentMessageType.Dropped, GetEntity(hand).Uid, hand);
                handslots.Remove(hand);
            }
        }

        private void DropAll()
        {
            Drop(Hand.Left);
            Drop(Hand.Right);
        }

        /// <summary>
        /// Check if the specified hand is empty
        /// </summary>
        /// <param name="hand"></param>
        /// <returns></returns>
        private bool IsEmpty(Hand hand)
        {
            if (handslots.ContainsKey(hand))
                return false;
            return true;
        }

    }
}
