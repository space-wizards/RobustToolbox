using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D.Atom.Object.Atmos
{
    class Vent : Object
    {
        public Vent()
            : base()
        {
            SetSpriteName(0, "Vent");
            SetSpriteByIndex(0);
            collidable = false;
        }
    }
}
