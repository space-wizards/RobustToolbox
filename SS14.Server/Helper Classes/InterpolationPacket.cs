using Lidgren.Network;
using SS14.Shared;
using System;
using SS14.Shared.Maths;
using SFML.System;

namespace SS14.Server.HelperClasses
{
    public struct InterpolationPacket
    {
        public Vector2f position;
        public float rotation;
        public double time;

        public InterpolationPacket(Vector2f _position, float _rotation, double _time)
        {
            position = _position;
            rotation = _rotation;
            time = _time;
        }

        public InterpolationPacket(float x, float y, float _rotation, double _time)
        {
            position = new Vector2f(x, y);
            rotation = _rotation;
            time = _time;
        }

        public InterpolationPacket(NetIncomingMessage message)
        {
            float x = message.ReadFloat();
            float y = message.ReadFloat();
            float z = message.ReadFloat();
            position = new Vector2f(x, y);
            rotation = message.ReadFloat();
            time = 0;
        }

        public void WriteMessage(NetOutgoingMessage message)
        {
            message.Write((float) Math.Round(position.X, 4));
            message.Write((float) Math.Round(position.Y, 4));
            message.Write((float) Math.Round(rotation, 4));
        }
    }
}