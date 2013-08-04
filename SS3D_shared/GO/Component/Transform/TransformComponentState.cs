using System;

namespace SS13_Shared.GO.Component.Transform
{
    [Serializable]
    public class TransformComponentState : ComponentState
    {
        public bool ForceUpdate;
        public float X;
        public float Y;

        public TransformComponentState(float x, float y, bool forceUpdate)
        {
            X = x;
            Y = y;
            ForceUpdate = forceUpdate;
            Family = ComponentFamily.Transform;
        }
    }
}