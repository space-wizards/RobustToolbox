using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CGO;

namespace SS3D.Atom.Object.WallMounted
{
    public class MedCabinet : WallMounted
    {
        public MedCabinet()
            : base()
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            ISpriteComponent c = (ISpriteComponent)GetComponent(SS3D_shared.GO.ComponentFamily.Renderable);
            c.AddSprite("med_cabinet");
            c.SetSpriteByKey("med_cabinet");
            collidable = false;
            snapTogrid = true;
        }
    }
}
