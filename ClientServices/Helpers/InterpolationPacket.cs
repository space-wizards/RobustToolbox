using Lidgren.Network;
using GorgonLibrary;

namespace ClientServices.Helpers
{
    public struct InterpolationPacket
    {
        public double time;
        public Vector2D position;
        public float rotation;
        public int iterations;
        public Vector2D startposition;

        public InterpolationPacket(Vector2D _position, float _rotation, double _time)
        {
            this.position = _position;
            this.rotation = _rotation;
            this.time = _time;
            iterations = 0;
            startposition = new Vector2D(1234, 1234);
        }

        public InterpolationPacket(float x, float y, float _rotation, double _time)
        {
            this.position = new Vector2D(x, y);
            this.rotation = _rotation;
            this.time = _time;
            iterations = 0;
            startposition = new Vector2D(1234, 1234);
        }

        public InterpolationPacket(NetIncomingMessage message)
        {
            float x = message.ReadFloat();
            float y = message.ReadFloat();
            position = new Vector2D(x, y);
            rotation = message.ReadFloat();
            time = 0;
            iterations = 0;
            startposition = new Vector2D(1234, 1234);
        }

        public void WriteMessage(NetOutgoingMessage message)
        {
            message.Write(position.X);
            message.Write(position.Y);
            message.Write(rotation);
        }

    }
}
