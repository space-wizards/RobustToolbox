using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO
{
    public class BasicInteractableComponent : GameObjectComponent
    {
        public BasicInteractableComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Interactable;
        }

        public override void RecieveMessage(object sender, MessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case MessageType.Click: //We were clicked, start the interaction
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
            actor.SendMessage(this, MessageType.IsCurrentHandEmpty, replies);
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
            // Does this ent have an actor component(is it a mob?) if so, the actor component should mediate this interaction
            if (Owner.HasComponent(SS3D_shared.GO.ComponentFamily.Actor))
                Owner.SendMessage(this, MessageType.ItemToActorInteraction, null, actor);

            //Does this ent have an item component? That item component should mediate this interaction
            if(Owner.HasComponent(SS3D_shared.GO.ComponentFamily.Item))
                Owner.SendMessage(this, MessageType.ItemToItemInteraction, null, actor);

            //if not, does this ent have a largeobject component? That component should mediate this interaction.
            else if(Owner.HasComponent(SS3D_shared.GO.ComponentFamily.LargeObject))
                Owner.SendMessage(this, MessageType.ItemToLargeObjectInteraction, null, actor);
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
                Owner.SendMessage(this, MessageType.EmptyHandToActorInteraction, null, actor);
            
            //If we can be picked up, do that -- ItemComponent mediates that
            if (Owner.HasComponent(SS3D_shared.GO.ComponentFamily.Item))
                Owner.SendMessage(this, MessageType.EmptyHandToItemInteraction, null, actor);

            //If not, can we be used? -- LargeObject does that
            else if (Owner.HasComponent(SS3D_shared.GO.ComponentFamily.LargeObject))
                Owner.SendMessage(this, MessageType.EmptyHandToLargeObjectInteraction, null, actor);

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
    }
}
