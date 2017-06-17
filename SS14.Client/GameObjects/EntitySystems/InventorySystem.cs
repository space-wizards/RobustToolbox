using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.IoC;

namespace SS14.Client.GameObjects.EntitySystems
{
    public class InventorySystem : EntitySystem
    {
        public InventorySystem()
        {
            EntityQuery = new EntityQuery();
        }
    }
}
