using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Mogre;
using Lidgren.Network;

namespace SS3D_shared.HelperClasses
{
    public struct InterpolationPacket
    {
        public double time;
        public Mogre.Vector3 position;
        public float rotW;
        public float rotY;

        public InterpolationPacket(Mogre.Vector3 _position, float _rotW, float _rotY, double _time)
        {
            this.position = _position;
            this.rotW = _rotW;
            this.rotY = _rotY;
            this.time = _time;
        }

        public InterpolationPacket(float x, float y, float z, float _rotW, float _rotY, double _time)
        {
            this.position = new Mogre.Vector3(x, y, z);
            this.rotW = _rotW;
            this.rotY = _rotY;
            this.time = _time;
        }

        public InterpolationPacket(NetIncomingMessage message)
        {
            float x = message.ReadFloat();
            float y = message.ReadFloat();
            float z = message.ReadFloat();
            position = new Mogre.Vector3(x, y, z);
            rotW = message.ReadFloat();
            rotY = message.ReadFloat();
            time = 0;
        }

    }
}
