using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D.Atom.Object
{
    class Locker : Object
    {
        public Locker()
            : base()
        {
            collidable = true;
            SetSpriteName(0, "Locker_closed");
            SetSpriteByIndex(0);
            SetSpriteName(1, "Locker_open");
        }
    }
}
