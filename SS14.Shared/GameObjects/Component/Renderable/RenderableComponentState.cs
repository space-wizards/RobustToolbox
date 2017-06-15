using System;

namespace SS14.Shared.GameObjects.Components.Renderable
{
    [Serializable]
    public class RenderableComponentState : ComponentState
    {
        public DrawDepth DrawDepth;
        public int? MasterUid;

        public RenderableComponentState(DrawDepth drawDepth, int? masterUid) :
            base(ComponentFamily.Renderable)
        {
            DrawDepth = drawDepth;
            MasterUid = masterUid;
        }
    }
}
