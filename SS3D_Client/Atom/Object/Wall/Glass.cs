using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using CGO;

namespace SS3D.Atom.Object.Wall
{
    class Glass : Object
    {
        public Glass()
            : base()
        {
            //SetSpriteName(0, "glass");
            //SetSpriteName(1, "glass_shattered");
            //SetSpriteByIndex(0);
            collidable = true;
            snapTogrid = true;

            ISpriteComponent c = (ISpriteComponent)GetComponent(SS3D_shared.GO.ComponentFamily.Renderable);
            c.AddSprite("glass");
            c.AddSprite("glass_shattered");
            c.SetSpriteByKey("glass");
        }

        public override RectangleF GetAABB()
        {
            return new RectangleF(position.X - ((sprite.Width * sprite.UniformScale) / 2),
            position.Y + ((sprite.Height * sprite.UniformScale) / 2) - 1,
            (sprite.Width * sprite.UniformScale),
            1);
        }
    }
}
