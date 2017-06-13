using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;

namespace SS14.Client.GameObjects.EntitySystems
{
    public class InventorySystem : EntitySystem
    {
        public InventorySystem(ClientEntityManager em, EntitySystemManager esm)
            : base(em, esm)
        {
            EntityQuery = new EntityQuery();
        }

        public override void Update(float frametime)
        {
        }
    }
}