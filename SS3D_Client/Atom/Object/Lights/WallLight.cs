using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using SS3D.Modules;

namespace SS3D.Atom.Object.Lights
{
    public class WallLight : Object
    {
        public WallLight()
            : base()
        {
            spritename = "WallLight";
            collidable = false;
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
            UpdatePosition();
            light.UpdateLight();
        }

        public override void UpdatePosition()
        {
            base.UpdatePosition();

            if (light == null)
                return;
            light.UpdatePosition(position + new Vector2D(0, 48));

        }
    }
}
