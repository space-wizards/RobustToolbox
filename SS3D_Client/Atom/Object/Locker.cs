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
            SetSpriteName(0, "locker_closed");
            SetSpriteByIndex(0);
            SetSpriteName(1, "locker_open");
        }
    }
}
