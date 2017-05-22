using System;

namespace SS14.Shared.GameObjects.Components.Renderable
{
    [Serializable]
    public class WearableAnimatedSpriteComponentState : AnimatedSpriteComponentState
    {
        public bool IsCurrentlyWorn;
        public bool IsCurrentlyCarried;

        public WearableAnimatedSpriteComponentState(bool isCurrentlyWorn, bool isCurrentlyCarried, bool visible, DrawDepth drawDepth, string name, string currentAnimation, bool loop, int? masterUid)
            : base(visible, drawDepth, name, currentAnimation, loop, masterUid)
        {
            IsCurrentlyWorn = isCurrentlyWorn;
            IsCurrentlyCarried = isCurrentlyCarried;
        }
    }
}
