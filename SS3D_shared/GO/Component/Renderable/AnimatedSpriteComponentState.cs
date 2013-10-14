using System;

namespace SS13_Shared.GO.Component.Renderable
{
    [Serializable]
    public class AnimatedSpriteComponentState : RenderableComponentState
    {
        public string Name;
        public bool Visible;
        public string CurrentAnimation;
        public bool Loop;

        public AnimatedSpriteComponentState(bool visible, DrawDepth drawDepth, string name, string currentAnimation, bool loop)
            : base(drawDepth)
        {
            Visible = visible;
            CurrentAnimation = currentAnimation;
            Loop = loop;
            Name = name;
        }
    }
}