using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CGO;

namespace SS3D.Atom.Item.Wearable.Inner.Jumpsuit
{
    public class Assistant_Grey : Jumpsuit
    {
        public Assistant_Grey()
            : base()
        {
            //SetSpriteName(-1, "jumpsuit");
            //SetSpriteByIndex(-1);
        }

        public override void Initialize()
        {
            base.Initialize();

            AddComponent(SS3D_shared.GO.ComponentFamily.Renderable, ComponentFactory.Singleton.GetComponent("WearableSpriteComponent"));
            IGameObjectComponent c = (IGameObjectComponent)GetComponent(SS3D_shared.GO.ComponentFamily.Renderable);
            c.SetParameter(new ComponentParameter("basename", typeof(string), "jumpsuit"));
        }
    }
}
