using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;
using SS13_Shared.GO;

namespace SGO
{
    public class BasicInteractableComponent : GameObjectComponent
    {
        public BasicInteractableComponent()
        {
            family = SS13_Shared.GO.ComponentFamily.Interactable;
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.Click: //We were clicked, start the interaction
                    if (list.Count() > 0)
                        HandleClick((int)list[0]);
                    break;

            }

            return reply;
        }

        /// <summary>
        /// Click handler
        /// </summary>
        /// <param name="actorID">the id of the actor that clicked us</param>
        private void HandleClick(int actorID)
        {
            Entity actor = EntityManager.Singleton.GetEntity(actorID);
            if (actor == null || !actor.HasComponent(SS13_Shared.GO.ComponentFamily.Actor)) // if actor is null or doesnt have actor component
                return; // whoops bail out.
            DoInteraction(actor);
        }

        /// <summary>
        /// Entry point for all entity-entity interactions.
        /// </summary>
        /// <param name="actor">The entity that is the actor.</param>
        private void DoInteraction(Entity actor)
        {
            //Determine what kind of interaction this is
            //Does the actor have hands?
            if (actor.HasComponent(SS13_Shared.GO.ComponentFamily.Hands))
            { // Actor has hands.
                DoHandsInteraction(actor);
            }
            else
            { // Actor doesn't have hands. All we can do is headbutt?
                DoNoHandsInteraction(actor);
            }
        }

        /// <summary>
        /// Entry point for interactions with actors that have hands
        /// </summary>
        /// <param name="actor"></param>
        protected virtual void DoHandsInteraction(Entity actor)
        {
            // Ask if the current hand is empty
            var reply = actor.SendMessage(this, ComponentFamily.Hands, ComponentMessageType.IsCurrentHandEmpty);
            if (reply.MessageType != ComponentMessageType.Empty && (bool)reply.ParamsList[0] == true)
            {
                DoEmptyHandInteraction(actor);
            }
            else if (reply.MessageType != ComponentMessageType.Empty && (bool)reply.ParamsList[0] == false)
            {
                DoHeldItemInteraction(actor);
            }
            
        }

        /// <summary>
        /// Does an interaction between the item held in the actor's currently selected hand and this entity
        /// Basically, someone uses an item on an entity.
        /// </summary>
        /// <param name="actor"></param>
        protected virtual void DoHeldItemInteraction(Entity actor)
        {
            Entity actingItem = GetItemInActorHand(actor);
            if (actingItem == null) // this should not happen
                return;

            // Does this ent have an actor component(is it a mob?) if so, the actor component should mediate this interaction
            if (Owner.HasComponent(SS13_Shared.GO.ComponentFamily.Actor))
            {
                Owner.SendMessage(this, ComponentMessageType.ReceiveItemToActorInteraction, actor);
                actingItem.SendMessage(this, ComponentMessageType.EnactItemToActorInteraction, Owner, actor);
            }

            //Does this ent have an item component? That item component should mediate this interaction
            else if (Owner.HasComponent(SS13_Shared.GO.ComponentFamily.Item))
            {
                Owner.SendMessage(this, ComponentMessageType.ReceiveItemToItemInteraction, actor);
                actingItem.SendMessage(this, ComponentMessageType.EnactItemToItemInteraction, Owner, actor);
            }

            //if not, does this ent have a largeobject component? That component should mediate this interaction.
            else if (Owner.HasComponent(SS13_Shared.GO.ComponentFamily.LargeObject))
            {
                Owner.SendMessage(this, ComponentMessageType.ReceiveItemToLargeObjectInteraction, actor);
                actingItem.SendMessage(this, ComponentMessageType.EnactItemToLargeObjectInteraction, Owner, actor);
            }
        }

        /// <summary>
        /// Does an empty hand interaction between the actor's empty hand and the object.
        /// Basically, someone touches an entity with an empty hand.
        /// </summary>
        /// <param name="actor"></param>
        protected virtual void DoEmptyHandInteraction(Entity actor)
        {            
            // Does this ent have an actor component(is it a mob?) if so, the actor component should mediate this interaction
            if (Owner.HasComponent(SS13_Shared.GO.ComponentFamily.Actor))
                Owner.SendMessage(this, ComponentMessageType.ReceiveEmptyHandToActorInteraction, actor);
            
            //If we can be picked up, do that -- ItemComponent mediates that
            else if (Owner.HasComponent(SS13_Shared.GO.ComponentFamily.Item))
                Owner.SendMessage(this, ComponentMessageType.ReceiveEmptyHandToItemInteraction, actor);

            //If not, can we be used? -- LargeObject does that
            else if (Owner.HasComponent(SS13_Shared.GO.ComponentFamily.LargeObject))
                Owner.SendMessage(this, ComponentMessageType.ReceiveEmptyHandToLargeObjectInteraction, actor);

            
        }

        /// <summary>
        /// Entry point for interactions with actors that don't have hands
        /// HEADBUTT FTW
        /// </summary>
        /// <param name="actor"></param>
        protected virtual void DoNoHandsInteraction(Entity actor)
        {
            //LOL WTF IS THIS SHIT
        }

        protected virtual Entity GetItemInActorHand(Entity actor)
        {
            var reply = actor.SendMessage(this, ComponentFamily.Hands, ComponentMessageType.GetActiveHandItem);
            if (reply.MessageType == ComponentMessageType.ReturnActiveHandItem && (reply.ParamsList[0].GetType().IsSubclassOf(typeof(Entity)) || reply.ParamsList[0].GetType() == typeof(Entity))) 
            {
                return (Entity)reply.ParamsList[0];
            }

            return null;
        }
    }
}
