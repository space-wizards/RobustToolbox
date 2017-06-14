using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;

namespace SS14.Client.GameObjects.EntitySystems
{
    [IoCTarget]
    public class ParticleSystem : EntitySystem
    {
        public ParticleSystem()
        {
            EntityQuery = new EntityQuery();
            EntityQuery.OneSet.Add(typeof(ParticleSystemComponent));
        }

        public void AddParticleSystem(IEntity ent, string systemName)
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

        public override void HandleNetMessage(EntitySystemMessage sysMsg)
        {
            base.HandleNetMessage(sysMsg);
        }
    }
}
