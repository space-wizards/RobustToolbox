using SS14.Shared.GameObjects.System;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.GameObjects;

namespace SS14.Client.GameObjects.EntitySystems
{
    sealed class AppearanceSystem : EntitySystem
    {
        [Dependency]
        IComponentManager componentManager;

        public AppearanceSystem()
        {
            IoCManager.InjectDependencies(this);

            EntityQuery = new ComponentEntityQuery
            {
                OneSet = new List<Type>
                {
                    typeof(AppearanceComponent),
                    typeof(AppearanceTestComponent),
                },
            };
        }

        public override void FrameUpdate(float frameTime)
        {
            foreach (var entity in EntityManager.GetEntities(EntityQuery))
            {
                if(entity.TryGetComponent<AppearanceComponent>(out var appearComp))
                {
                    if (appearComp.AppearanceDirty)
                    {
                        UpdateComponent(appearComp);
                        appearComp.AppearanceDirty = false;
                    }
                }
                else if (entity.TryGetComponent<AppearanceTestComponent>(out var testComp))
                {
                    testComp.OnUpdate(frameTime);
                }
            }
        }

        static void UpdateComponent(AppearanceComponent component)
        {
            foreach (var visualizer in component.Visualizers)
            {
                switch (visualizer)
                {
                    case AppearanceComponent.SpriteLayerToggle spriteLayerToggle:
                        UpdateSpriteLayerToggle(component, spriteLayerToggle);
                        break;

                    default:
                        visualizer.OnChangeData(component);
                        break;
                }
            }
        }

        static void UpdateSpriteLayerToggle(AppearanceComponent component, AppearanceComponent.SpriteLayerToggle toggle)
        {
            component.TryGetData(toggle.Key, out bool visible);
            var sprite = component.Owner.GetComponent<SpriteComponent>();
            sprite.LayerSetVisible(toggle.SpriteLayer, visible);
        }
    }
}
