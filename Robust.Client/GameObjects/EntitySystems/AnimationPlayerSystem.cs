using Robust.Client.GameObjects.Components.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Robust.Client.GameObjects.EntitySystems
{
    internal sealed class AnimationPlayerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(AnimationPlayerComponent));
        }

        public override void FrameUpdate(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var player = entity.GetComponent<AnimationPlayerComponent>();
                player.Update(frameTime);
            }
        }
    }
}
