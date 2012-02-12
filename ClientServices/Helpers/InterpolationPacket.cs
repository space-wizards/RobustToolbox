using Lidgren.Network;
using GorgonLibrary;

namespace ClientServices.Helpers
{
    public struct InterpolationPacket
    {
        public double Time;
        public Vector2D Position;
        public float Rotation;
        public int Iterations;
        public Vector2D Startposition;

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
            var x = message.ReadFloat();
            var y = message.ReadFloat();
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
