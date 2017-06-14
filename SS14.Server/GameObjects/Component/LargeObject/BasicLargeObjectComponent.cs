using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System.Linq;

namespace SS14.Server.GameObjects
{
    [IoCTarget]
    public class BasicLargeObjectComponent : Component
    {
        public override string Name => "BasicLargeObject";
        public BasicLargeObjectComponent()
        {
            Family = ComponentFamily.LargeObject;
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.ReceiveEmptyHandToLargeObjectInteraction:
                    HandleEmptyHandToLargeObjectInteraction((IEntity)list[0]);
                    break;
                case ComponentMessageType.ReceiveItemToLargeObjectInteraction:
                    HandleItemToLargeObjectInteraction((IEntity)list[0]);
                    break;
            }

            return reply;
        }

        /// <summary>
        /// Entry point for interactions between an item and this object
        /// Basically, the actor uses an item on this object
        /// </summary>
        /// <param name="actor">the actor entity</param>
        protected void HandleItemToLargeObjectInteraction(IEntity actor)
        {
            //Get the item
            ComponentReplyMessage reply = actor.SendMessage(this, ComponentFamily.Hands,
                                                            ComponentMessageType.GetActiveHandItem);

            if (reply.MessageType != ComponentMessageType.ReturnActiveHandItem)
                return; // No item in actor's active hand. This shouldn't happen.

            var item = (Entity)reply.ParamsList[0];

            reply = item.SendMessage(this, ComponentFamily.Item, ComponentMessageType.ItemGetCapabilityVerbPairs);
            if (reply.MessageType == ComponentMessageType.ItemReturnCapabilityVerbPairs)
            {
                var verbs = (Lookup<ItemCapabilityType, ItemCapabilityVerb>)reply.ParamsList[0];
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
        protected virtual void RecieveItemInteraction(IEntity actor, IEntity item,
                                                      Lookup<ItemCapabilityType, ItemCapabilityVerb> verbs)
        {
        }

        /// <summary>
        /// Recieve an item interaction. woop. NO VERBS D:
        /// </summary>
        /// <param name="item"></param>
        protected virtual void RecieveItemInteraction(IEntity actor, IEntity item)
        {
        }

        /// <summary>
        /// Entry point for interactions between an empty hand and this object
        /// Basically, actor "uses" this object
        /// </summary>
        /// <param name="actor">The actor entity</param>
        protected virtual void HandleEmptyHandToLargeObjectInteraction(IEntity actor)
        {
            //Apply actions
        }
    }
}
