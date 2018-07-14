using SS14.Shared.GameObjects.System;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.GameObjects.EntitySystems
{
    sealed class AppearanceSystem : EntitySystem
    {
        [Dependency]
        IComponentManager componentManager;

        public AppearanceSystem()
        {
            IoCManager.InjectDependencies(this);
        }

        public override void FrameUpdate(float frameTime)
        {
            foreach (var component in componentManager.GetComponents<AppearanceComponent>())
            {
                if (component.AppearanceDirty)
                {
                    UpdateComponent(component);
                    component.AppearanceDirty = false;
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
