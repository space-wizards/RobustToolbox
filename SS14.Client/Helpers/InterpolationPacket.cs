using Lidgren.Network;
using OpenTK;

namespace SS14.Client.Helpers
{
    public struct InterpolationPacket
    {
        private int iterations;
        private Vector2 position;
        private float rotation;
        private Vector2 startposition;
        private double time;

        public int Iterations { get => iterations; set => iterations = value; }
        public Vector2 Position { get => position; set => position = value; }
        public float Rotation { get => rotation; set => rotation = value; }
        public Vector2 Startposition { get => startposition; set => startposition = value; }
        public double Time { get => time; set => time = value; }

        public InterpolationPacket(Vector2 position, float rotation, double time) : this()
        {
            Position = position;
            Rotation = rotation;
            Time = time;
            Iterations = 0;
            Startposition = new Vector2(1234, 1234);
        }

        public InterpolationPacket(float x, float y, float rotation, double time) : this()
        {
            Position = new Vector2(x, y);
            Rotation = rotation;
            Time = time;
            Iterations = 0;
            Startposition = new Vector2(1234, 1234);
        }

        public InterpolationPacket(NetIncomingMessage message) : this()
        {
            float x = message.ReadFloat();
            float y = message.ReadFloat();
            Position = new Vector2(x, y);
            Rotation = message.ReadFloat();
            Time = 0;
            Iterations = 0;
            Startposition = new Vector2(1234, 1234);
        }

        public void WriteMessage(NetOutgoingMessage message)
        {
            message.Write(Position.X);
            message.Write(Position.Y);
            message.Write(Rotation);
        }
    }
}
