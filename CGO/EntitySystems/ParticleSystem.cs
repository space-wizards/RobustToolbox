using CGO;
using ClientInterfaces.Resource;
using GameObject;
using GameObject.System;
using SS13.IoC;
using SS13_Shared.GO;
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
            ParticleSettings settings = IoCManager.Resolve<IResourceManager>().GetParticles(systemName);
            if (settings != null)
            {
                //Add it.
            }
        }

        public override void RegisterMessageTypes()
        {
            //EntitySystemManager.RegisterMessageType<>();
            base.RegisterMessageTypes();
        }

        public override void Update(float frametime)
        {
        }

        public override void HandleNetMessage(EntitySystemMessage sysMsg)
        {
            base.HandleNetMessage(sysMsg);
        }
    }
}