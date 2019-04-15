using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Robust.Client.GameObjects.EntitySystems
{
    class AppearanceTestSystem : EntitySystem
    {
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(AppearanceTestComponent));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var comp = entity.GetComponent<AppearanceTestComponent>();
                comp.OnUpdate(frameTime);
            }
        }
    }
}
