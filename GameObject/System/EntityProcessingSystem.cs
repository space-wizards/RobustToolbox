using System.Collections.Generic;
using SS13_Shared;
using SS13_Shared.GO;

namespace GameObject.System
{
    public abstract class EntityProcessingSystem : EntitySystem
    {
        public EntityProcessingSystem(EntityManager em, EntitySystemManager esm):base(em, esm)
        {}

        public override void Update(float frameTime)
        {}
    }
}