using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D.Atom.Item.Tool
{
    public class Wrench : Tool
    {
        public Wrench()
            : base()
        {
            SetSpriteName(0, "Wrench");
            SetSpriteByIndex(0);
        }
    }
}
