using System;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class AnimatedSpriteComponentState : RenderableComponentState
    {
        public readonly string CurrentAnimation;
        public readonly bool Loop;
        public readonly string Name;
        public readonly bool Visible;

        public AnimatedSpriteComponentState(bool visible, DrawDepth drawDepth, string name,
            string currentAnimation, bool loop, EntityUid? masterUid, uint netID = NetIDs.ANIMATED_SPRITE)
            : base(drawDepth, masterUid, netID)
        {
            Visible = visible;
            CurrentAnimation = currentAnimation;
            Loop = loop;
            Name = name;
        }
    }
}
