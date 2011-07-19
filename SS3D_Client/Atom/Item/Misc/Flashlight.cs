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
            spritename = "Flashlight.png";
        }

        public override void HandlePush(Lidgren.Network.NetIncomingMessage message)
        {
            base.HandlePush(message);
            int r = (int)message.ReadByte();
            int g = (int)message.ReadByte();
            int b = (int)message.ReadByte();
            LightDirection d = (LightDirection)message.ReadByte();
            if (light == null)
            {
                light = new Light(atomManager.gameState.map, Color.FromArgb(r, g, b), 190, LightState.On, atomManager.gameState.map.GetTileArrayPositionFromWorldPosition(position), d);
            }
            else
            {
                light.color = Color.FromArgb(r, g, b);
            }
            light.UpdateLight();
        }

        public override void UpdatePosition()
        {
            base.UpdatePosition();
            
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
