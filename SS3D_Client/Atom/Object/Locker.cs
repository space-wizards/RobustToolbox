using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CGO;

namespace SS3D.Atom.Object
{
    class Locker : Object
    {
        public Locker()
            : base()
        {

        }

        public override void Initialize()
        {
            base.Initialize();

            collidable = true;
            //TODO: port this shit --
            // Server side sprite name/indexing needs to be ported to use components
            /*SetSpriteName(0, "locker_closed");
            SetSpriteByIndex(0);
            SetSpriteName(1, "locker_open");*/
            ISpriteComponent c = (ISpriteComponent)GetComponent(SS3D_shared.GO.ComponentFamily.Renderable);
            c.AddSprite("locker_closed");
            c.AddSprite("locker_open");
            c.SetSpriteByKey("locker_closed");
        }

    }
}
