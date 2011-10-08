using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using SS3D.Modules;
using ClientServices.Lighting;
using CGO;

namespace SS3D.Atom.Object.Lights
{
    public class WallLight : Object
    {
        public WallLight()
            : base()
        {

        }

        public override void Initialize()
        {
            base.Initialize();
            ISpriteComponent c = (ISpriteComponent)GetComponent(SS3D_shared.GO.ComponentFamily.Renderable);
            c.AddSprite("wall_light");
            c.SetSpriteByKey("wall_light");
            AddComponent(SS3D_shared.GO.ComponentFamily.Light, ComponentFactory.Singleton.GetComponent("PointLightComponent"));
            collidable = false;
        }
    }
}
