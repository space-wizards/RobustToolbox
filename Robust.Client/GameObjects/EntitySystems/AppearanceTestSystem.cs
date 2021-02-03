using Robust.Shared.GameObjects.Systems;

namespace Robust.Client.GameObjects.EntitySystems
{
    class AppearanceTestSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var appearanceTestComponent in EntityManager.ComponentManager.EntityQuery<AppearanceTestComponent>(true))
            {
                appearanceTestComponent.OnUpdate(frameTime);
            }
        }
    }
}
