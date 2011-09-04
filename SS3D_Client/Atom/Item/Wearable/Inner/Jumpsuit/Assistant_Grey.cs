using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D.Atom.Item.Wearable.Inner.Jumpsuit
{
    public class Assistant_Grey : Jumpsuit
    {
        public Assistant_Grey()
            : base()
        {
            SetSpriteName(-1, "jumpsuit");
            SetSpriteName(0, "jumpsuit_front");
            SetSpriteName(2, "jumpsuit_back");
            SetSpriteByIndex(-1);
        }
    }
}
