using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;

namespace SS14.Server.GameObjects.EntitySystems
{
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
