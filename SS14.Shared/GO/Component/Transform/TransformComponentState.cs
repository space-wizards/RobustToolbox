using System;

namespace SS14.Shared.GO.Component.Transform
{
    [Serializable]
    public class TransformComponentState : ComponentState
    {
        public bool ForceUpdate;
        public float X;
        public float Y;

        public TransformComponentState(float x, float y, bool forceUpdate)
            : base(ComponentFamily.Transform)
        {
            X = x;
            Y = y;
            ForceUpdate = forceUpdate;
        }
    }
}