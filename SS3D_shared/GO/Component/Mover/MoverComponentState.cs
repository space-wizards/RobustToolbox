
using System;

namespace SS13_Shared.GO.Component.Mover
{
    [Serializable]
    public class MoverComponentState : ComponentState
    {
        public double X;
        public double Y;
        public MoverComponentState(double x, double y)
        {
            X = x;
            Y = y;
            Family = ComponentFamily.Mover;
        }
    }
}
