using CGO;
using GameObject;
using GameObject.System;
using EntityManager = GameObject.EntityManager;

namespace CGO.EntitySystems
{
    public class TestSystem : EntitySystem
    {
        public TestSystem(EntityManager em, EntitySystemManager esm)
            : base(em, esm)
        {
            EntityQuery = new EntityQuery();
            EntityQuery.OneSet.Add(typeof (BasicItemComponent));
        }

        public override void Update(float frametime)
        {
        }
    }
}