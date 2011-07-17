using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lidgren.Network;
using GorgonLibrary;
using GorgonLibrary.Graphics;

namespace SS3D.HelperClasses
{
    public struct InterpolationPacket
    {
        public double time;
        public Vector2D position;
        public float rotW;
        public float rotY;
        public int iterations;
        public Vector2D startposition;

        public InterpolationPacket(Vector2D _position, float _rotW, float _rotY, double _time)
        {
            this.position = _position;
            this.rotW = _rotW;
            this.rotY = _rotY;
            this.time = _time;
            iterations = 0;
            startposition = new Vector2D(1234, 1234);
        }

        public InterpolationPacket(float x, float y, float _rotW, float _rotY, double _time)
        {
            this.position = new Vector2D(x, y);
            this.rotW = _rotW;
            this.rotY = _rotY;
            this.time = _time;
            iterations = 0;
            startposition = new Vector2D(1234, 1234);
        }

        public InterpolationPacket(NetIncomingMessage message)
        {
            float x = message.ReadFloat();
            float y = message.ReadFloat();
            position = new Vector2D(x, y);
            rotW = message.ReadFloat();
            rotY = message.ReadFloat();
            time = 0;
            iterations = 0;
            startposition = new Vector2D(1234, 1234);
        }

        public void WriteMessage(NetOutgoingMessage message)
        {
            message.Write(position.X);
            message.Write(position.Y);
            message.Write(rotW);
            message.Write(rotY);
        }

    }
}
