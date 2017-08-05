using System;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class RenderableComponentState : ComponentState
    {
        public readonly DrawDepth DrawDepth;
        public readonly int? MasterUid;

        public RenderableComponentState(DrawDepth drawDepth, int? masterUid, uint netID) :
            base(netID)
        {
            DrawDepth = drawDepth;
            MasterUid = masterUid;
        }
    }
}
