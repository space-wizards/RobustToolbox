using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;

namespace SS14.Client.GameObjects.EntitySystems
{
    public class InventorySystem : EntitySystem
    {
        public InventorySystem()
        {
            EntityQuery = new EntityQuery();
        }

        public override void Update(float frametime)
        {
        }
    }
}
