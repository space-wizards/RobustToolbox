using Robust.Client.Interfaces.Resource;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;

namespace Robust.Client.GameObjects.EntitySystems
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
