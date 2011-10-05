using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO.Component.Hands
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

        public override void RecieveMessage(object sender, MessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case MessageType.IsCurrentHandEmpty:
                    replies.Add(new ComponentReplyMessage(MessageType.IsCurrentHandEmpty, IsEmpty(currentHand)));
                    break;
            }
        }



        private void SwitchHands()
        {
            if (currentHand == Hand.Left)
                currentHand = Hand.Right;
            else
                currentHand = Hand.Left;
        }

        private Entity GetEntity(Hand hand)
        {
            if (!IsEmpty(hand))
                return handslots[hand];
            else 
                return null;
        }

        private Hand GetCurrentHand()
        { return currentHand; }

        private void SetEntity(Hand hand, Entity entity)
        {
            if (entity != null && IsEmpty(hand))
                handslots[hand] = entity;
        }

        private void Pickup(Entity entity)
        {
            if (entity != null && IsEmpty(currentHand))
                SetEntity(currentHand, entity);
        }

        private bool IsEmpty(Hand hand)
        {
            if (handslots[hand] == null)
                return true;
            return false;
        }

    }
}
