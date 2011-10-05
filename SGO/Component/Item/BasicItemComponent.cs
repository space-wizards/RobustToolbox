using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO.Component.Item
{
    public class BasicItemComponent : GameObjectComponent
    {


        public BasicItemComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Item;
        }

        public override void RecieveMessage(object sender, MessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case MessageType.EmptyHandToItemInteraction:
                    HandleEmptyHandToItemInteraction((Entity)list[0]); // param 0 is the actor entity
                    break;
                case MessageType.ItemToItemInteraction:
                    HandleItemToItemInteraction((Entity)list[0]); // param 0 is the actor entity
                    break;
            }

        }

        /// <summary>
        /// Entry point for interactions between an item and this item
        /// Basically, the actor uses an item on this item
        /// </summary>
        /// <param name="entity">The actor entity</param>
        protected virtual void HandleItemToItemInteraction(Entity actor)
        {
            //Get the item

            //Apply actions based on the item's types
            //Message the item to tell it to apply whatever it needs to do as well
        }

        /// <summary>
        /// Entry point for interactions between an empty hand and this item
        /// Basically, the actor touches this item with an empty hand
        /// </summary>
        /// <param name="actor"></param>
        protected virtual void HandleEmptyHandToItemInteraction(Entity actor)
        {
            //Pick up the item
        }
    }
}
