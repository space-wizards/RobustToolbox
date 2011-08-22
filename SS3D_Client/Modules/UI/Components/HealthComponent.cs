using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;

using Lidgren.Network;

using SS3D_shared;

namespace SS3D.Modules.UI.Components
{
    public class HumanHealthComponent : IGuiComponent
    {
        public GuiComponent componentClass
        {
            get;
            set;
        }


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
                baseSprite.SetPosition(position.X, position.Y);
                healthAmount.SetPosition(position.X + 7, position.Y + 16);
            }
        }
        public Sprite baseSprite;
        public TextSprite healthAmount;
        public GorgonLibrary.Graphics.Font healthDisplayFont;


        public HumanHealthComponent()
        {
            componentClass = GuiComponent.HealthComponent;
        
            baseSprite = ResMgr.Singleton.GetSpriteFromImage("healthgreen");

            healthDisplayFont = new GorgonLibrary.Graphics.Font("Arial8pt", "Arial", 8.0f, true, true);
            healthAmount = new TextSprite("healthAmount", "100%", healthDisplayFont);
            healthAmount.Color = System.Drawing.Color.Black;

            Position = new Point(Gorgon.Screen.Width - 42, Gorgon.Screen.Height - 99);
        }

        public void Render()
        {
            baseSprite.Draw();
            healthAmount.Draw();
        }

        public void HandleNetworkMessage(NetIncomingMessage message)
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
    }
}
