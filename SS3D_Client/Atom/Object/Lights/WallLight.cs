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
            SetSpriteName(0, "WallLight");
            SetSpriteByIndex(0);
            collidable = false;
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
                //light = new Light(atomManager.gameState.map, Color.FromArgb(r, g, b), 300, LightState.On, atomManager.gameState.map.GetTileArrayPositionFromWorldPosition(position), d);
                light = new Light(atomManager.gameState.map, Color.FromArgb(r, g, b), 300, LightState.On, position, d);
                light.brightness = 1.5f;
            }
            else
            {
                light.color = Color.FromArgb(r, g, b);
                light.brightness = 1.5f;
            }
           
            UpdatePosition();
            light.UpdateLight();
        }

        public override void  Render(float xTopLeft, float yTopLeft)
        {
 	         
            if (light != null)
            {
                switch (light.direction[0])
                {
                    case Direction.North:
                        sprite.Rotation = 180;
                        break;
                    case Direction.East:
                        sprite.Rotation = 270;
                        break;
                    case Direction.South:
                        sprite.Rotation = 0;
                        break;
                    case Direction.West:
                        sprite.Rotation = 90;
                        break;
                    case Direction.All:
                        sprite.Rotation = 0;
                        break;
                    default:
                        break;
                }
            }
            base.Render(xTopLeft, yTopLeft);
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
