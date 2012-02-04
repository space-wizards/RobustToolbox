using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared.GO;
using ServerServices.Map;
using ServerServices;
using ServerInterfaces;
using SS13_Shared.HelperClasses;

namespace SGO
{
    public class WorktopComponent : BasicLargeObjectComponent
    {
        public WorktopComponent()
            :base()
        {
            family = SS13_Shared.GO.ComponentFamily.LargeObject;
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            base.RecieveMessage(sender, type, replies, list);
        }

        public override void Update(float frameTime)
        {
        }

        private void PlaceItem(Entity actor, Entity item)
        {
            Random rnd = new Random();
            actor.SendMessage(this, ComponentMessageType.DropItemInCurrentHand, null); //Should drop item that was used on us? Maybe add more precise message later.
            item.SendMessage(this, ComponentMessageType.SetDrawDepth, null, (int)DrawDepth.ItemsOnTables);
            item.Translate(Owner.position + new Vector2(rnd.Next(-28, 28),rnd.Next(-28, 15)) );
        }

        protected override void RecieveItemInteraction(Entity actor, Entity item, Lookup<ItemCapabilityType, ItemCapabilityVerb> verbs)
        {
            base.RecieveItemInteraction(actor, item, verbs);

            if (verbs[ItemCapabilityType.Tool].Contains(ItemCapabilityVerb.Wrench))
            {
            }
            else
                PlaceItem(actor, item);
        }

        /// <summary>
        /// Recieve an item interaction. woop. NO VERBS D:
        /// </summary>
        /// <param name="item"></param>
        protected override void RecieveItemInteraction(Entity actor, Entity item)
        {
            PlaceItem(actor, item);
        }

        /// <summary>
        /// Entry point for interactions between an empty hand and this object
        /// Basically, actor "uses" this object
        /// </summary>
        /// <param name="actor">The actor entity</param>
        protected override void HandleEmptyHandToLargeObjectInteraction(Entity actor)
        {
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);
        }
    }
}
