using GorgonLibrary;
using Lidgren.Network;

namespace SS14.Client.Services.Helpers
{
    public struct InterpolationPacket
    {
        public int Iterations;
        public Vector2D Position;
        public float Rotation;
        public Vector2D Startposition;
        public double Time;

        public InterpolationPacket(Vector2D position, float rotation, double time)
        {
            Position = position;
            Rotation = rotation;
            Time = time;
            Iterations = 0;
            Startposition = new Vector2D(1234, 1234);
        }

        public InterpolationPacket(float x, float y, float rotation, double time)
        {
            Position = new Vector2D(x, y);
            Rotation = rotation;
            Time = time;
            Iterations = 0;
            Startposition = new Vector2D(1234, 1234);
        }

        public InterpolationPacket(NetIncomingMessage message)
        {
            float x = message.ReadFloat();
            float y = message.ReadFloat();
            Position = new Vector2D(x, y);
            Rotation = message.ReadFloat();
            Time = 0;
            Iterations = 0;
            Startposition = new Vector2D(1234, 1234);
        }

        public void WriteMessage(NetOutgoingMessage message)
        {
            message.Write(Position.X);
            message.Write(Position.Y);
            message.Write(Rotation);
        }
    }
}