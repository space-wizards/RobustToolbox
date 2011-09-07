using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D.Atom.Item.Wearable.Head
{
    public class Helmet : Head
    {
        public Helmet()
            : base()
        {
            SetSpriteName(-1, "helmet");
            SetSpriteName(0, "helmet_front");
            SetSpriteName(2, "helmet_back");
            SetSpriteByIndex(-1);
        }




    }
}
