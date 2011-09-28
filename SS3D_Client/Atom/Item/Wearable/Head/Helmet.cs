using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CGO;

namespace SS3D.Atom.Item.Wearable.Head
{
    public class Helmet : Head
    {
        public Helmet()
            : base()
        {
            //SetSpriteName(-1, "helmet");
            //SetSpriteByIndex(-1);
            AddComponent(SS3D_shared.GO.ComponentFamily.Renderable, ComponentFactory.Singleton.GetComponent("WearableSpriteComponent"));
            IGameObjectComponent c = (IGameObjectComponent)GetComponent(SS3D_shared.GO.ComponentFamily.Renderable);
            c.SetParameter(new ComponentParameter("basename", typeof(string), "helmet"));
        }




    }
}
