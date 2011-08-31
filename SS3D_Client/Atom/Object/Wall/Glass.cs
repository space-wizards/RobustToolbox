using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace SS3D.Atom.Object.Wall
{
    class Glass : Object
    {
        public Glass()
            : base()
        {
            spritename = "glass";
            collidable = true;
            snapTogrid = true;
        }

        public override RectangleF GetAABB()
        {
            return new RectangleF(position.X - ((sprite.Width * sprite.UniformScale) / 2),
            position.Y + ((sprite.Height * sprite.UniformScale) / 2) - 1,
            (sprite.Width * sprite.UniformScale),
            1);
        }
    }
}
