using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CGO;

namespace SS3D.Atom.Object.WallMounted
{
    public class Extinguisher : WallMounted
    {
        public Extinguisher()
            : base()
        {
            ISpriteComponent c = (ISpriteComponent)GetComponent(SS3D_shared.GO.ComponentFamily.Renderable);
            c.AddSprite("fire_extinguisher");
            c.SetSpriteByKey("fire_extinguisher");
            collidable = false;
            snapTogrid = true;
        }
    }
}
