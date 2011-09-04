using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;

namespace SS3D.Atom.Item.Wearable.Feet
{
    public class Shoes : Feet
    {
        public Shoes()
            : base()
        {
            SetSpriteName(-1, "shoes");
            SetSpriteName(0, "shoes_front");
            SetSpriteName(2, "shoes_back");
            SetSpriteByIndex(-1);
        }

    }
}
