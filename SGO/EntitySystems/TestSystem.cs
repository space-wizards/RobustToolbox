using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameObject;

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

        public override void Update(float frametime)
        {
        }
    }
}
