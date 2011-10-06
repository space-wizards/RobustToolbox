using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using SS3D.Modules;
using ClientLighting;
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
        }

        public override void HandlePush(Lidgren.Network.NetIncomingMessage message)
        {
            base.HandlePush(message);
            int r = (int)message.ReadByte();
            int g = (int)message.ReadByte();
            int b = (int)message.ReadByte();
            Direction d = (Direction)message.ReadByte();
            if (light == null)
            {
                light = new Light(atomManager.gameState.map, Color.FromArgb(r, g, b), 250, LightState.On, position, d);
            }
            else
            {
                light.color = Color.FromArgb(r, g, b);
            }
            UpdatePosition();
            light.UpdateLight();
        }

        public override void UpdatePosition()
        {
            base.UpdatePosition();

            if (light == null)
                return;
            if (holdingAppendage != null)
            {
                light.UpdatePosition(holdingAppendage.owner.position);
            }
            else
            {
                light.UpdatePosition(position);
            }
        }

    }
}
