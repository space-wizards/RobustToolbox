using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;

namespace SS14.Client.GameObjects.EntitySystems
{
    public class SpriteSystem : EntitySystem
    {
        public SpriteSystem()
        {
            EntityQuery = new TypeEntityQuery(typeof(ISpriteComponent));
        }

        public override void FrameUpdate(float frameTime)
        {
            foreach (var entity in EntityManager.GetEntities(EntityQuery))
            {
                // TODO: Don't call this on components without RSIs loaded.
                // Serious performance benefit here.
                entity.GetComponent<ISpriteComponent>().FrameUpdate(frameTime);
            }
        }
    }
}
