using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO.Component.Interactable
{
    public class BasicInteractableComponent : GameObjectComponent
    {

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

        }

        /// <summary>
        /// Entry point for interactions with actors that don't have hands
        /// HEADBUTT FTW
        /// </summary>
        /// <param name="actor"></param>
        protected virtual void DoNoHandsInteraction(Entity actor)
        {

        }
    }
}
