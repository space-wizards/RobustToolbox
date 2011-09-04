using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D.Atom.Object.WallMounted
{
    public class Extinguisher : WallMounted
    {
        public Extinguisher()
            : base()
        {
            SetSpriteName(0, "fire_extinguisher");
            SetSpriteByIndex(0);
            collidable = false;
            snapTogrid = true;
        }
    }
}
