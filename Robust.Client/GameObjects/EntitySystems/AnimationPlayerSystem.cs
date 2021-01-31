using Robust.Client.GameObjects.Components.Animations;
using Robust.Shared.GameObjects.Systems;

namespace Robust.Client.GameObjects.EntitySystems
{
    internal sealed class AnimationPlayerSystem : EntitySystem
    {
        public override void FrameUpdate(float frameTime)
        {
            foreach (var animationPlayerComponent in EntityManager.ComponentManager.EntityQuery<AnimationPlayerComponent>(true))
            {
                animationPlayerComponent.Update(frameTime);
            }
        }
    }
}
