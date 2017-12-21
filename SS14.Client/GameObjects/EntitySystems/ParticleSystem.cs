using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;

namespace SS14.Client.GameObjects.EntitySystems
{
    public class ParticleSystem : EntitySystem
    {
        public ParticleSystem()
        {
            EntityQuery = new ComponentEntityQuery()
            {
                OneSet = new List<Type>()
                {
                    typeof(ParticleSystemComponent),
                },
            };
        }

        public void AddParticleSystem(IEntity ent, string systemName)
        {
            ParticleSettings settings = IoCManager.Resolve<IResourceCache>().GetParticles(systemName);
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
