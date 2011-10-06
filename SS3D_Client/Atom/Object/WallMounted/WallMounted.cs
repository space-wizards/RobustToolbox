using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace SS3D.Atom.Object.WallMounted
{
    public abstract class WallMounted : Object
    {
        public WallMounted()
            : base()
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            spritename = "worktop";
            collidable = false;
            snapTogrid = true;
        }
    }
}
