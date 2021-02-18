using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects
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
