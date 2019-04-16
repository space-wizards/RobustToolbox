using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;

namespace Robust.Server.GameObjects.EntitySystems
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

        public override void RegisterMessageTypes()
        {
            base.RegisterMessageTypes();
        }

        public override void Update(float frametime)
        {
            //TODO: Figure out what to do with this
        }
    }
}
