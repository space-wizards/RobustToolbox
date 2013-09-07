using System.Collections.Generic;
using System.Linq;
using GameObject;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
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
    }
}