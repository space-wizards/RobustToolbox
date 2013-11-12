using CGO;
using ClientInterfaces.Resource;
using GameObject;
using GameObject.System;
using SS13.IoC;
using EntityManager = GameObject.EntityManager;

namespace CGO.EntitySystems
{
    public class ParticleSystem : EntitySystem
    {
        public ParticleSystem(EntityManager em, EntitySystemManager esm)
            : base(em, esm)
        {
            EntityQuery = new EntityQuery();
            EntityQuery.OneSet.Add(typeof(ParticleSystemComponent));
        }

        public void AddParticleSystem(Entity ent, string systemName)
        {
            //ParticleSystem = IoCManager.Resolve<IResourceManager>().Ge;
        }

        public override void Update(float frametime)
        {
        }
    }
}