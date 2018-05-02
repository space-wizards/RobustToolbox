using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.GameObjects.System;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;

namespace SS14.Client.GameObjects.EntitySystems
{
    public class SpriteSystem : EntitySystem
    {
        [Dependency]
        IComponentManager componentManager;

        public SpriteSystem()
        {
            IoCManager.InjectDependencies(this);
        }

        public override void FrameUpdate(float frameTime)
        {
            foreach (var component in componentManager.GetComponents<ISpriteComponent>())
            {
                // TODO: Don't call this on components without RSIs loaded.
                // Serious performance benefit here.
                component.FrameUpdate(frameTime);
            }
        }
    }
}
