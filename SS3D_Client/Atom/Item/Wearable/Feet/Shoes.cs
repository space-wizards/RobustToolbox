using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using CGO;

namespace SS3D.Atom.Item.Wearable.Feet
{
    public class Shoes : Feet
    {
        public Shoes()
            : base()
        {
            //SetSpriteName(-1, "shoes");
            //SetSpriteByIndex(-1);
            AddComponent(SS3D_shared.GO.ComponentFamily.Renderable, ComponentFactory.Singleton.GetComponent("WearableSpriteComponent"));
            IGameObjectComponent c = (IGameObjectComponent)GetComponent(SS3D_shared.GO.ComponentFamily.Renderable);
            c.SetParameter(new ComponentParameter("basename", typeof(string), "shoes"));
        }

    }
}
