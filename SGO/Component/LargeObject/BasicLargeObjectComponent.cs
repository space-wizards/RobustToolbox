using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared.GO;
using SGO.Component.Item.ItemCapability;

namespace SGO
{
    public class BasicLargeObjectComponent: GameObjectComponent
    {
        public BasicLargeObjectComponent()
        {
            family = SS13_Shared.GO.ComponentFamily.LargeObject;
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case ComponentMessageType.ReceiveEmptyHandToLargeObjectInteraction:
                    HandleEmptyHandToLargeObjectInteraction((Entity)list[0]);
                    break;
                case ComponentMessageType.ReceiveItemToLargeObjectInteraction:
                    HandleItemToLargeObjectInteraction((Entity)list[0]);
                    break;
            }
        }

        /// <summary>
        /// Entry point for interactions between an item and this object
        /// Basically, the actor uses an item on this object
        /// </summary>
        /// <param name="actor">the actor entity</param>
        protected void HandleItemToLargeObjectInteraction(Entity actor)
        {
            //Get the item
            List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
            actor.SendMessage(this, ComponentMessageType.GetActiveHandItem, replies);

            if(replies.Count == 0 || replies[0].messageType != ComponentMessageType.ReturnActiveHandItem)
                return; // No item in actor's active hand. This shouldn't happen.

            Entity item = (Entity)replies[0].paramsList[0];
            replies.Clear();
            item.SendMessage(this, ComponentMessageType.ItemGetCapabilityVerbPairs, replies);
            if (replies.Count > 0 && replies[0].messageType == ComponentMessageType.ItemReturnCapabilityVerbPairs)
            {
                var verbs = (Lookup<ItemCapabilityType, ItemCapabilityVerb>)replies[0].paramsList[0];
                if (verbs.Count == 0 || verbs == null)
                    RecieveItemInteraction(actor, item);
                else
                    RecieveItemInteraction(actor, item, verbs);
            }
        }

        /// <summary>
        /// Recieve an item interaction. woop.
        /// </summary>
        /// <param name="item"></param>
        protected virtual void RecieveItemInteraction(Entity actor, Entity item, Lookup<ItemCapabilityType, ItemCapabilityVerb> verbs)
        {
        }

        /// <summary>
        /// Recieve an item interaction. woop. NO VERBS D:
        /// </summary>
        /// <param name="item"></param>
        protected virtual void RecieveItemInteraction(Entity actor, Entity item)
        {
        }

        /// <summary>
        /// Entry point for interactions between an empty hand and this object
        /// Basically, actor "uses" this object
        /// </summary>
        /// <param name="actor">The actor entity</param>
        protected virtual void HandleEmptyHandToLargeObjectInteraction(Entity actor)
        {
            //Apply actions
        }
    }
}
