using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3d_server.Atom.Item
{
    public class Light : Item
    {
        public Color color;
        public LightDirection direction;

        public Light(Color _color, LightDirection _direction)
            : base()
        {
            color = _color;
            direction = _direction;
        }
    }

    public struct Color
    {
        public byte r;
        public byte g;
        public byte b;

        public Color(byte _r, byte _g, byte _b)
        {
            r = _r;
            g = _g;
            b = _b;
        }
    }

}
