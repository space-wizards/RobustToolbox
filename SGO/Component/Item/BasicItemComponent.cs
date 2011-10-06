using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;

namespace SGO
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
                case MessageType.PickedUp:
                    HandlePickedUp((Entity)list[0]);
                    break;
                case MessageType.Dropped:
                    HandleDropped();
                    break;
            }

        }

        private void HandleDropped()
        {
            Owner.RemoveComponent(ComponentFamily.Mover);
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, ItemComponentNetMessage.Dropped);
        }

        private void HandlePickedUp(Entity entity)
        {
            Owner.AddComponent(SS3D_shared.GO.ComponentFamily.Mover, ComponentFactory.Singleton.GetComponent("SlaveMoverComponent"));
            Owner.SendMessage(this, MessageType.SlaveAttach, null, entity.Uid);
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, ItemComponentNetMessage.PickedUp, entity.Uid);
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
            actor.SendMessage(this, MessageType.PickUpItem, null, Owner);
        }
    }
}
