using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using SS3D.Modules;
using ClientServices.Lighting;
using CGO;

namespace SS3D.Atom.Item.Misc
{
    public class Flashlight : Item
    {
        public Flashlight()
            : base()
        {
            //SetSpriteName(-1,  "flashlight");
            //SetSpriteByIndex(-1);
        }

        public override void Initialize()
        {
            base.Initialize();
            AddComponent(SS3D_shared.GO.ComponentFamily.Renderable, ComponentFactory.Singleton.GetComponent("ItemSpriteComponent"));
            IGameObjectComponent c = (IGameObjectComponent)GetComponent(SS3D_shared.GO.ComponentFamily.Renderable);
            c.SetParameter(new ComponentParameter("basename", typeof(string), "flashlight"));
            AddComponent(SS3D_shared.GO.ComponentFamily.Light, ComponentFactory.Singleton.GetComponent("PointLightComponent"));
        }
    }
}
