using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using System.Collections.Generic;

namespace SS14.Client.GameObjects
{
    public class NewInventoryComponent : Component
    {
        public NewInventoryComponent()
        {
            Family = ComponentFamily.Inventory;
            ContainedEntities = new List<Entity>();
        }

        public List<Entity> ContainedEntities { get; private set; }

        public int MaxSlots { get; private set; }

        public override void HandleComponentState(dynamic state)
        {
            List<Entity> newContents = new List<Entity>();
            foreach (int uid in state.ContainedEntities)
            {
                Entity retEnt = Owner.EntityManager.GetEntity(uid);
                newContents.Add(retEnt);
            }
            MaxSlots = state.MaxSlots;

            //check for differences and raise event later?

            ContainedEntities = newContents;
        }
    }
}