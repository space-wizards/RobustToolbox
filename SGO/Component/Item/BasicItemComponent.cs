using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;
using SGO.Component.Item.ItemCapability;

namespace SGO
{
    public class BasicItemComponent : GameObjectComponent
    {
        private Entity currentHolder;

        private Dictionary<string, ItemCapability> capabilities;

        public BasicItemComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Item;
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case ComponentMessageType.ReceiveEmptyHandToItemInteraction:
                    HandleEmptyHandToItemInteraction((Entity)list[0]); // param 0 is the actor entity
                    break;
                case ComponentMessageType.ReceiveItemToItemInteraction: //This message means we were clicked on by an acter with an item in hand
                    HandleItemToItemInteraction((Entity)list[0]); // param 0 is the actor entity
                    break;
                case ComponentMessageType.EnactItemToActorInteraction:
                    ApplyTo((Entity)list[0], InteractsWith.Actor);
                    break;
                case ComponentMessageType.EnactItemToItemInteraction:
                    ApplyTo((Entity)list[0], InteractsWith.Item);
                    break;
                case ComponentMessageType.EnactItemToLargeObjectInteraction:
                    ApplyTo((Entity)list[0], InteractsWith.LargeObject);
                    break;
                case ComponentMessageType.PickedUp:
                    HandlePickedUp((Entity)list[0]);
                    break;
                case ComponentMessageType.Dropped:
                    HandleDropped();
                    break;
                case ComponentMessageType.GetCapability:
                    break;
            }

        }

        /// <summary>
        /// Applies this item to the target entity. 
        /// </summary>
        /// <param name="targetEntity">Target entity</param>
        /// <param name="targetType">Type of entity, Item, LargeObject, or Actor</param>
        protected virtual void ApplyTo(Entity targetEntity, InteractsWith targetType)
        {
            //can be overridden in children to sort of bypass the capability system if needed.
            ApplyCapabilities(targetEntity, targetType);
        }

        private void HandleDropped()
        {
            Owner.RemoveComponent(ComponentFamily.Mover);
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, ItemComponentNetMessage.Dropped);
            currentHolder = null;
        }

        private void HandlePickedUp(Entity entity)
        {
            currentHolder = entity;
            Owner.AddComponent(SS3D_shared.GO.ComponentFamily.Mover, ComponentFactory.Singleton.GetComponent("SlaveMoverComponent"));
            Owner.SendMessage(this, ComponentMessageType.SlaveAttach, null, entity.Uid);
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, ItemComponentNetMessage.PickedUp, entity.Uid);
        }

        public override void HandleInstantiationMessage(Lidgren.Network.NetConnection netConnection)
        {
            if(currentHolder != null)
                Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, netConnection, ItemComponentNetMessage.PickedUp, currentHolder.Uid);
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
            actor.SendMessage(this, ComponentMessageType.PickUpItem, null, Owner);
        }


        /// <summary>
        /// Apply this item's capabilities to a target entity
        /// This finds all onboard capability modules that can interact with a given object type, 
        /// sorted by priority. Only one thing will actually execute, depending on priority. 
        /// ApplyTo returns true if it successfully interacted with the target, false if not.
        /// </summary>
        /// <param name="target">Target entity for interaction</param>
        protected virtual void ApplyCapabilities(Entity target, InteractsWith targetType)
        {
            var capstoapply = from c in capabilities.Values
                              where (c.interactsWith & targetType) == targetType
                              orderby c.priority descending
                              select c;
                              
            foreach (ItemCapability capability in capstoapply)
            {
                if (capability.ApplyTo(target))
                    break;
            }
        }
    }
}
