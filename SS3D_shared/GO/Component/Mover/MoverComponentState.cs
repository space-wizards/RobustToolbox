using System;

namespace SS13_Shared.GO.Component.Mover
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
        {
            X = x;
            Y = y;
            VelocityX = velx;
            VelocityY = vely;
            Family = ComponentFamily.Mover;
        }

        public MoverComponentState(float x, float y, float velx, float vely, int master)
        {
            X = x;
            Y = y;
            VelocityX = velx;
            VelocityY = vely;
            Family = ComponentFamily.Mover;
            Master = master;
        }
    }
}