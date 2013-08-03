using System;
using System.Linq;
using SS13_Shared;
using SS13_Shared.GO;

namespace SGO
{
    public class WorktopComponent : BasicLargeObjectComponent
    {
        public WorktopComponent()
        {
            Family = ComponentFamily.LargeObject;
        }

        public override void Update(float frameTime)
        {
        }

        private void PlaceItem(GameObject.Entity actor, GameObject.Entity item)
        {
            var rnd = new Random();
            actor.SendMessage(this, ComponentMessageType.DropItemInCurrentHand);
            item.GetComponent<SpriteComponent>(ComponentFamily.Renderable).drawDepth = DrawDepth.ItemsOnTables; //TODO Unsafe, fix.
            item.GetComponent<TransformComponent>(ComponentFamily.Transform).TranslateByOffset(new Vector2(rnd.Next(-28, 28), rnd.Next(-28, 15)));
        }

        protected override void RecieveItemInteraction(GameObject.Entity actor, GameObject.Entity item,
                                                       Lookup<ItemCapabilityType, ItemCapabilityVerb> verbs)
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
        protected override void RecieveItemInteraction(GameObject.Entity actor, GameObject.Entity item)
        {
            PlaceItem(actor, item);
        }

        /// <summary>
        /// Entry point for interactions between an empty hand and this object
        /// Basically, actor "uses" this object
        /// </summary>
        /// <param name="actor">The actor entity</param>
        protected override void HandleEmptyHandToLargeObjectInteraction(GameObject.Entity actor)
        {
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);
        }
    }
}