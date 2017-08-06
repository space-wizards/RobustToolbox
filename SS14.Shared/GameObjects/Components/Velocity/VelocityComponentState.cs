using System;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class VelocityComponentState : ComponentState
    {
        public readonly float VelocityX;
        public readonly float VelocityY;

        public VelocityComponentState(float velx, float vely)
            : base(NetIDs.VELOCITY)
        {
            VelocityX = velx;
            VelocityY = vely;
        }
    }
}
