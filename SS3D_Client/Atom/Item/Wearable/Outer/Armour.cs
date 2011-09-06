using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;

namespace SS3D.Atom.Item.Wearable.Outer
{
    public class Armour : Outer
    {
        public Armour()
            : base()
        {
            SetSpriteName(-1, "armour_front");
            SetSpriteName(0, "armour_front");
            SetSpriteName(2, "armour_front");
            SetSpriteByIndex(-1);
        }

    }
}
