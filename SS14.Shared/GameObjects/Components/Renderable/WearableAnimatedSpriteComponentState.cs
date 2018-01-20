using System;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class WearableAnimatedSpriteComponentState : AnimatedSpriteComponentState
    {
        public readonly bool IsCurrentlyWorn;
        public readonly bool IsCurrentlyCarried;

        public WearableAnimatedSpriteComponentState(bool isCurrentlyWorn, bool isCurrentlyCarried,
            bool visible, DrawDepth drawDepth, string name, string currentAnimation, bool loop, EntityUid? masterUid)
            : base(visible, drawDepth, name, currentAnimation, loop, masterUid, NetIDs.WEARABLE_ANIMATED_SPRITE)
        {
            IsCurrentlyWorn = isCurrentlyWorn;
            IsCurrentlyCarried = isCurrentlyCarried;
        }
    }
}
