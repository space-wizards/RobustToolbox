using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using CGO;

namespace SS3D.Atom.Item.Wearable.Outer
{
    public class Armour : Outer
    {
        public Armour()
            : base()
        {
            //SetSpriteName(-1, "armour");
            //SetSpriteByIndex(-1);
        }

        public override void Initialize()
        {
            base.Initialize();
            AddComponent(SS3D_shared.GO.ComponentFamily.Renderable, ComponentFactory.Singleton.GetComponent("WearableSpriteComponent"));
            IGameObjectComponent c = (IGameObjectComponent)GetComponent(SS3D_shared.GO.ComponentFamily.Renderable);
            c.SetParameter(new ComponentParameter("basename", typeof(string), "armour"));
        }

    }
}
