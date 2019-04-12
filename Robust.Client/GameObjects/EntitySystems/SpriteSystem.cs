using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.GameObjects.EntitySystems
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
