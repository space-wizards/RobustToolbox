using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using Lidgren.Network;
using SS3D.Atom.Mob.HelperClasses;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using CGO;
using SS3D_shared.GO;

namespace SS3D.Atom.Item
{
    public abstract class Item : Atom
    {
        public Appendage holdingAppendage = null;
                public Item()
            : base()
        {
            
        }

        public override void Initialize()
        {
            base.Initialize();
            AddComponent(SS3D_shared.GO.ComponentFamily.Item, ComponentFactory.Singleton.GetComponent("BasicItemComponent"));
        }

        public override void Draw()
        {
            base.Draw();

            /*if (spriteNames.ContainsKey(-1))
            {
                string baseName = spriteNames[-1];
                SetSpriteName(0, baseName + "_front");
                SetSpriteName(1, baseName + "_side");
                SetSpriteName(2, baseName + "_back");
                SetSpriteName(3, baseName + "_side");
                SetSpriteName(5, baseName + "_inhand");
            }*/
        }
    }
}
