using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;

namespace SGO
{
    public class BasicLargeObjectComponent: GameObjectComponent
    {
        public BasicLargeObjectComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.LargeObject;
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case ComponentMessageType.EmptyHandToLargeObjectInteraction:
                    HandleEmptyHandToLargeObjectInteraction((Entity)list[0]);
                    break;
                case ComponentMessageType.ItemToLargeObjectInteraction:
                    HandleItemToLargeObjectInteraction((Entity)list[0]);
                    break;
            }
        }

        /// <summary>
        /// Entry point for interactions between an item and this object
        /// Basically, the actor uses an item on this object
        /// </summary>
        /// <param name="actor">the actor entity</param>
        protected virtual void HandleItemToLargeObjectInteraction(Entity actor)
        {
            //Get the item
            //Get item type infos to apply to this object
            //Message item to tell it it was applied to this object
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
