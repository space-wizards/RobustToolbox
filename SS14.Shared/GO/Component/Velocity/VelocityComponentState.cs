using System;

namespace SS14.Shared.GO.Component.Velocity
{
    [Serializable]
    public class VelocityComponentState : ComponentState
    {
        public float VelocityX;
        public float VelocityY;

        public VelocityComponentState(float velx, float vely)
            : base(ComponentFamily.Velocity)
        {
            VelocityX = velx;
            VelocityY = vely;
        }
    }
}