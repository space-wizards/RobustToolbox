using SS14.Client.GameObjects.Components.Animations;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;

namespace SS14.Client.GameObjects.EntitySystems
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
