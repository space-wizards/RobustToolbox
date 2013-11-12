using GameObject;
using GameObject.System;

namespace SGO.EntitySystems
{
    public class ParticleSystem : EntitySystem
    {
        public ParticleSystem(EntityManager em, EntitySystemManager esm)
            : base(em, esm)
        {
            EntityQuery = new EntityQuery();
            EntityQuery.OneSet.Add(typeof(ParticleSystemComponent));
        }

        public override void Update(float frametime)
        {
        }
    }
}