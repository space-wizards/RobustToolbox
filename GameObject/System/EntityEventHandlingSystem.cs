using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameObject.System
{
    public class EntityEventHandlingSystem : EntitySystem
    {
        public EntityEventHandlingSystem(EntityManager em, EntitySystemManager esm):base(em, esm)
        {}
    }
}
