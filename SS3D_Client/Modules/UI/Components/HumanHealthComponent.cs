using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

using Lidgren.Network;

using SS3D_shared;

namespace SS3D.Modules.UI.Components
{
    public class HumanHealthComponent : GuiComponent
    {
        private Point position;
        public Point Position
        {
            get
            {
                return position;
            }
            private set
            {
                position = value;
                greenSprite.SetPosition(position.X, position.Y);
                yellowSprite.SetPosition(position.X, position.Y);
                redSprite.SetPosition(position.X, position.Y);
                healthAmount.SetPosition(position.X + 7, position.Y + 16);
            }
        }
        private Sprite baseSprite;
        private Sprite greenSprite;
        private Sprite yellowSprite;
        private Sprite redSprite;
        private TextSprite healthAmount;
        private GorgonLibrary.Graphics.Font healthDisplayFont;


        public HumanHealthComponent(PlayerController _playerController)
            :base(_playerController)
        {
            componentClass = GuiComponentType.HealthComponent;
        
            greenSprite = ResMgr.Singleton.GetSpriteFromImage("healthgreen");
            yellowSprite = ResMgr.Singleton.GetSpriteFromImage("healthyellow");
            redSprite = ResMgr.Singleton.GetSpriteFromImage("healthred");
            baseSprite = greenSprite;

            healthDisplayFont = new GorgonLibrary.Graphics.Font("Arial8pt", "Arial", 8.0f, true, true);
            healthAmount = new TextSprite("healthAmount", "100%", healthDisplayFont);
            healthAmount.Color = System.Drawing.Color.Black;

            Position = new Point(Gorgon.Screen.Width - 42, Gorgon.Screen.Height - 99);
        }

        public override void Render()
        {
            baseSprite.Draw();
            healthAmount.Draw();
        }

        public override void HandleNetworkMessage(NetIncomingMessage message)
        {
            HealthComponentMessage messageType = (HealthComponentMessage)message.ReadByte();
            switch (messageType)
            {
                case HealthComponentMessage.CurrentHealth:
                    SetHealthPercentage(message.ReadInt32());
                    break;
                default: break;
            }
        }

        public void SetHealthPercentage(int pct)
        {
            healthAmount.Text = pct.ToString() + "%";
        }

        public override void MouseDown(MouseInputEventArgs e)
        {
            System.Drawing.RectangleF mouseAABB = new System.Drawing.RectangleF(e.Position.X, e.Position.Y, 1, 1);
            if (baseSprite.AABB.IntersectsWith(mouseAABB))
            {
                if (baseSprite == greenSprite)
                    baseSprite = yellowSprite;
                else if (baseSprite == yellowSprite)
                    baseSprite = redSprite;
                else if (baseSprite == redSprite)
                    baseSprite = greenSprite;
            }
        }
    }
}
