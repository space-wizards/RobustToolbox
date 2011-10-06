using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared;

namespace SGO
{
    public class HumanHandsComponent : GameObjectComponent
    {
        private Dictionary<Hand, Entity> handslots;
        private Hand currentHand = Hand.Left;
        public enum Hand
        {
            Left,
            Right
        }

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
        public override void RecieveMessage(object sender, MessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case MessageType.IsCurrentHandEmpty:
                    replies.Add(new ComponentReplyMessage(MessageType.IsCurrentHandEmpty, IsEmpty(currentHand)));
                    break;
                case MessageType.PickUpItem:
                    Pickup((Entity)list[0]);
                    break;
                case MessageType.BoundKeyChange:
                    if ((BoundKeyFunctions)list[0] == BoundKeyFunctions.Drop)
                        Drop();
                    break;
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
                handslots.Add(hand, entity);
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
                entity.SendMessage(this, MessageType.PickedUp, null, Owner);
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
                GetEntity(hand).SendMessage(this, MessageType.Dropped, null);
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
