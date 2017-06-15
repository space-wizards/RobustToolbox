using System;

namespace SS14.Shared.GameObjects.Components.Renderable
{
    [Serializable]
    public class AnimatedSpriteComponentState : RenderableComponentState
    {
        public string Name;
        public bool Visible;
        public string CurrentAnimation;
        public bool Loop;

        public AnimatedSpriteComponentState(bool visible, DrawDepth drawDepth, string name, string currentAnimation, bool loop, int? masterUid)
            : base(drawDepth, masterUid)
        {
            Visible = visible;
            CurrentAnimation = currentAnimation;
            Loop = loop;
            Name = name;
        }
    }
}
