using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;

namespace SS14.Client.GameObjects.EntitySystems
{
    public class InventorySystem : EntitySystem
    {
        public InventorySystem(EntityManager em, EntitySystemManager esm)
            : base(em, esm)
        {
            EntityQuery = new EntityQuery();
            EntityQuery.OneSet.Add(typeof(NewInventoryComponent));
            EntityQuery.OneSet.Add(typeof(NewEquipmentComponent));
            EntityQuery.OneSet.Add(typeof(NewHandsComponent));
        }

        public override void Update(float frametime)
        {
        }
    }
}