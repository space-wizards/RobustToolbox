using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D.Atom.Object.WallMounted
{
    public class MedCabinet : WallMounted
    {
        public MedCabinet()
            : base()
        {
            SetSpriteName(0, "med_cabinet");
            SetSpriteByIndex(0);
            collidable = false;
            snapTogrid = true;
        }
    }
}
