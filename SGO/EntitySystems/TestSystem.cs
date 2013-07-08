using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO.EntitySystems
{
    public class TestSystem : EntitySystem
    {
        public TestSystem(EntityManager em)
            :base(em)
        {
            EntityQuery = new EntityQuery();
            EntityQuery.OneSet.Add(typeof(BasicItemComponent));
            //EntityQuery.Exclusionset.Add(typeof (LightComponent));
        }

        public override void Update()
        {
            foreach(var e in RelevantEntities)
            {
                var s = e.GetEntityState();
            }
        }
    }
}
