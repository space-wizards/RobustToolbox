using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using System.Security;

namespace CGO
{
    [SecurityCritical]
    public class SpriteComponent : RenderableComponent
    {
        Sprite s;

        public SpriteComponent()
        {
            s = new Sprite("blah");
        }
    }
}
