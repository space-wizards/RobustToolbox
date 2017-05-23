using System;

namespace SS14.Shared.GameObjects.Components.Mover
{
    [Serializable]
    public class MoverComponentState : ComponentState
    {
        public float VelocityX;
        public float VelocityY;
        public float X;
        public float Y;
        public int? Master;

        public MoverComponentState(float x, float y, float velx, float vely)
            :base(ComponentFamily.Mover)
        {
            X = x;
            Y = y;
            VelocityX = velx;
            VelocityY = vely;
        }

        public MoverComponentState(float x, float y, float velx, float vely, int master)
            : base(ComponentFamily.Mover)
        {
            X = x;
            Y = y;
            VelocityX = velx;
            VelocityY = vely;
            Master = master;
        }
    }
}
