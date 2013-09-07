using CGO;
using GameObject;
using GameObject.System;
using EntityManager = GameObject.EntityManager;

namespace CGO.EntitySystems
{
    public class InventorySystem : EntitySystem
    {
        public InventorySystem(EntityManager em)
            : base(em)
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