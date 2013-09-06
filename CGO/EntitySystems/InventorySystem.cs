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
            EntityQuery.OneSet.Add(typeof(InventoryComponent));
            EntityQuery.OneSet.Add(typeof(EquipmentComponent));
            EntityQuery.OneSet.Add(typeof(HumanHandsComponent));
        }

        public override void Update(float frametime)
        {
        }
    }
}