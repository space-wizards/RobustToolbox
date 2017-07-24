using System;

namespace SS14.Shared.GameObjects.Components.Mover
{
    [Serializable]
    public class SlaveMoverComponentState : ComponentState
    {
        public float VelocityX;
        public float VelocityY;
        public float X;
        public float Y;
        public int? Master;

        public SlaveMoverComponentState(float x, float y, float velx, float vely)
            :base(NetIDs.SLAVE_MOVER)
        {
            X = x;
            Y = y;
            VelocityX = velx;
            VelocityY = vely;
        }

        public SlaveMoverComponentState(float x, float y, float velx, float vely, int master)
            : base(NetIDs.SLAVE_MOVER)
        {
            X = x;
            Y = y;
            VelocityX = velx;
            VelocityY = vely;
            Master = master;
        }
    }
}
