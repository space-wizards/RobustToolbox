using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;

namespace SGO
{
    public class BasicInteractableComponent : GameObjectComponent
    {
        public BasicInteractableComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Interactable;
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case ComponentMessageType.Click: //We were clicked, start the interaction
                    if (list.Count() > 0)
                        HandleClick((int)list[0]);
                    break;

            }

        }

        /// <summary>
        /// Click handler
        /// </summary>
        /// <param name="actorID">the id of the actor that clicked us</param>
        private void HandleClick(int actorID)
        {
            Entity actor = EntityManager.Singleton.GetEntity(actorID);
            if (actor == null || !actor.HasComponent(SS3D_shared.GO.ComponentFamily.Actor)) // if actor is null or doesnt have actor component
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
            if (actor.HasComponent(SS3D_shared.GO.ComponentFamily.Hands))
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
            List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
            actor.SendMessage(this, ComponentMessageType.IsCurrentHandEmpty, replies);
            if (replies.Count() > 0 && (bool)replies.First().paramsList[0] == true)
            {
                DoEmptyHandInteraction(actor);
            }
            else if (replies.Count() > 0 && (bool)replies.First().paramsList[0] == false)
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
            if (Owner.HasComponent(SS3D_shared.GO.ComponentFamily.Actor))
            {
                Owner.SendMessage(this, ComponentMessageType.ReceiveItemToActorInteraction, null, actor);
                actingItem.SendMessage(this, ComponentMessageType.EnactItemToActorInteraction, null, Owner);
            }

            //Does this ent have an item component? That item component should mediate this interaction
            else if (Owner.HasComponent(SS3D_shared.GO.ComponentFamily.Item))
            {
                Owner.SendMessage(this, ComponentMessageType.ReceiveItemToItemInteraction, null, actor);
                actingItem.SendMessage(this, ComponentMessageType.EnactItemToItemInteraction, null, Owner);
            }

            //if not, does this ent have a largeobject component? That component should mediate this interaction.
            else if (Owner.HasComponent(SS3D_shared.GO.ComponentFamily.LargeObject))
            {
                Owner.SendMessage(this, ComponentMessageType.ReceiveItemToLargeObjectInteraction, null, actor);
                actingItem.SendMessage(this, ComponentMessageType.EnactItemToLargeObjectInteraction, null, Owner);
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
            if (Owner.HasComponent(SS3D_shared.GO.ComponentFamily.Actor))
                Owner.SendMessage(this, ComponentMessageType.ReceiveEmptyHandToActorInteraction, null, actor);
            
            //If we can be picked up, do that -- ItemComponent mediates that
            else if (Owner.HasComponent(SS3D_shared.GO.ComponentFamily.Item))
                Owner.SendMessage(this, ComponentMessageType.ReceiveEmptyHandToItemInteraction, null, actor);

            //If not, can we be used? -- LargeObject does that
            else if (Owner.HasComponent(SS3D_shared.GO.ComponentFamily.LargeObject))
                Owner.SendMessage(this, ComponentMessageType.ReceiveEmptyHandToLargeObjectInteraction, null, actor);

            
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
            List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
            actor.SendMessage(this, ComponentMessageType.GetActiveHandItem, replies);
            foreach (ComponentReplyMessage reply in replies)
            {
                if (reply.messageType == ComponentMessageType.ReturnActiveHandItem && (reply.paramsList[0].GetType().IsSubclassOf(typeof(Entity)) || reply.paramsList[0].GetType() == typeof(Entity))) 
                {
                    return (Entity)reply.paramsList[0];
                }
            }
            return null;
        }
    }
}
