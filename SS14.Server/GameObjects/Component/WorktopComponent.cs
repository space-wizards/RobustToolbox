using SFML.System;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System;
using System.Linq;

namespace SS14.Server.GameObjects
{
    public class WorktopComponent : BasicLargeObjectComponent
    {
        public override string Name => "Worktop";
        public WorktopComponent()
        {
            Family = ComponentFamily.LargeObject;
        }

        public override void Update(float frameTime)
        {
        }

        private void PlaceItem(IEntity actor, IEntity item)
        {
            var rnd = new Random();
            actor.SendMessage(this, ComponentMessageType.DropItemInCurrentHand);
            item.GetComponent<SpriteComponent>(ComponentFamily.Renderable).drawDepth = DrawDepth.ItemsOnTables;
            //TODO Unsafe, fix.
            item.GetComponent<TransformComponent>(ComponentFamily.Transform).TranslateByOffset(
                new Vector2f(rnd.Next(-28, 28), rnd.Next(-28, 15)));
        }

        protected override void RecieveItemInteraction(IEntity actor, IEntity item,
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
        protected override void RecieveItemInteraction(IEntity actor, IEntity item)
        {
            PlaceItem(actor, item);
        }

        /// <summary>
        /// Entry point for interactions between an empty hand and this object
        /// Basically, actor "uses" this object
        /// </summary>
        /// <param name="actor">The actor entity</param>
        protected override void HandleEmptyHandToLargeObjectInteraction(IEntity actor)
        {
        }
    }
}
