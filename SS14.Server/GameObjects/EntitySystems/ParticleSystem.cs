using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.IoC;

namespace SS14.Server.GameObjects.EntitySystems
{
    [IoCTarget]
    public class ParticleSystem : EntitySystem
    {
        public ParticleSystem()
        {
            EntityQuery = new EntityQuery();
            EntityQuery.OneSet.Add(typeof(ParticleSystemComponent));
        }

        public override void RegisterMessageTypes()
        {
            //EntitySystemManager.RegisterMessageType<>();
            base.RegisterMessageTypes();
        }

        public override void Update(float frametime)
        {
        }
    }
}
