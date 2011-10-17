using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CGO;

namespace SS3D.Atom.Object.Atmos
{
    class Vent : Object
    {
        public Vent()
            : base()
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            ISpriteComponent c = (ISpriteComponent)GetComponent(SS3D_shared.GO.ComponentFamily.Renderable);
            c.AddSprite("vent");
            c.SetSpriteByKey("vent");
            collidable = false;
        }
    }
}
