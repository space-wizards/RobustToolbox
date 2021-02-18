using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects
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
