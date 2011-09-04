using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using SS3D.Modules;

namespace SS3D.Atom.Item.Misc
{
    public class Flashlight : Item
    {
        public Flashlight()
            : base()
        {
            SetSpriteName(-1,  "Flashlight");
            SetSpriteByIndex(-1);
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
