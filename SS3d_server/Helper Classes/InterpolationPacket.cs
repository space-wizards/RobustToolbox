using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SS3D_shared.HelperClasses;
using Lidgren.Network;

namespace SS3d_server.HelperClasses
{
    public struct InterpolationPacket
    {
        public double time;
        public Vector2 position;
        public float rotW;
        public float rotY;

        public InterpolationPacket(Vector2 _position, float _rotW, float _rotY, double _time)
        {
            this.position = _position;
            this.rotW = _rotW;
            this.rotY = _rotY;
            this.time = _time;
        }

        public InterpolationPacket(float x, float y, float _rotW, float _rotY, double _time)
        {
            this.position = new Vector2(x, y);
            this.rotW = _rotW;
            this.rotY = _rotY;
            this.time = _time;
        }

        public InterpolationPacket(NetIncomingMessage message)
        {
            float x = message.ReadFloat();
            float y = message.ReadFloat();
            float z = message.ReadFloat();
            position = new Vector2(x, y);
            rotW = message.ReadFloat();
            rotY = message.ReadFloat();
            time = 0;
        }

        public void WriteMessage(NetOutgoingMessage message)
        {
            message.Write((float)Math.Round(position.X, 4));
            message.Write((float)Math.Round(position.Y, 4));
            message.Write((float)Math.Round(rotW, 4));
            message.Write((float)Math.Round(rotY, 4));
        }

    }
}
