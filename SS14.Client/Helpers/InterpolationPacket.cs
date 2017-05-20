using Lidgren.Network;
using SFML.System;

namespace SS14.Client.Helpers
{
    public struct InterpolationPacket
    {
        public int Iterations;
        public Vector2f Position;
        public float Rotation;
        public Vector2f Startposition;
        public double Time;

        public InterpolationPacket(Vector2f position, float rotation, double time)
        {
            Position = position;
            Rotation = rotation;
            Time = time;
            Iterations = 0;
            Startposition = new Vector2f(1234, 1234);
        }

        public InterpolationPacket(float x, float y, float rotation, double time)
        {
            Position = new Vector2f(x, y);
            Rotation = rotation;
            Time = time;
            Iterations = 0;
            Startposition = new Vector2f(1234, 1234);
        }

        public InterpolationPacket(NetIncomingMessage message)
        {
            float x = message.ReadFloat();
            float y = message.ReadFloat();
            Position = new Vector2f(x, y);
            Rotation = message.ReadFloat();
            Time = 0;
            Iterations = 0;
            Startposition = new Vector2f(1234, 1234);
        }

        public void WriteMessage(NetOutgoingMessage message)
        {
            message.Write(Position.X);
            message.Write(Position.Y);
            message.Write(Rotation);
        }
    }
}