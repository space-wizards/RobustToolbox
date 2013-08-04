using System;

namespace SS13_Shared.GO.Component.Renderable
{
    [Serializable]
    public class RenderableComponentState : ComponentState
    {
        public DrawDepth DrawDepth;

        public RenderableComponentState(DrawDepth drawDepth) :
            base(ComponentFamily.Renderable)
        {
            DrawDepth = drawDepth;
        }
    }
}