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
    public class StatHealthComponent : GuiComponent
    {
        public override Point Position
        {
            get
            {
                return base.Position;
            }
            set
            {
                base.Position = value;
            }
        }

        private Sprite backgroundSprite;
        private Sprite healthDetail;
        private GorgonLibrary.Graphics.Font font;
        private TextSprite healthText;
        private Color backgroundColor;
        private int health;
        public Point size;
        private float step;
        private Vector2D blipPosition;
        private float blipXRelative;
        private float blipHeight;
        private float blipWidth;
        private float blipStart;

        public StatHealthComponent(PlayerController _playerController, Point _size)
            : base(_playerController)
        {
            //componentClass = SS3D_shared.GuiComponentType.???;
            backgroundSprite = ResMgr.Singleton.GetSprite("1pxwhite");
            healthDetail = ResMgr.Singleton.GetSprite("stat_health_detail");
            font = ResMgr.Singleton.GetFont("CALIBRI");
            healthText = new TextSprite("statHealthText", "Healthy", font);
            healthText.Color = Color.DarkBlue;
            size = _size;
            health = 100;
            SetBackgroundColor();
            blipPosition = new Vector2D(Position.X, Position.Y + ((size.Y / 3) * 2));
            step = size.X / 100;
            blipXRelative = 0;
            blipHeight = size.Y / 1.2f;
            blipWidth = size.X / 4;
            blipStart = size.X / 3;
            healthDetail.Position = new Vector2D(Position.X + 1, Position.Y + 1);
        }

        public override void HandleNetworkMessage(NetIncomingMessage message)
        {
            HealthComponentMessage messageType = (HealthComponentMessage)message.ReadByte();
            switch (messageType)
            {
                case HealthComponentMessage.CurrentHealth:
                    health = message.ReadInt32();
                    SetBackgroundColor();
                    break;
                default: break;
            }
        }

        private void SetBackgroundColor()
        {
            int red = 255 - (int)Math.Round(health * 2.5f);
            int green = (int)Math.Round(health * 2.5f);
            int blue = 50;

            backgroundColor = Color.FromArgb(red, green, blue);
        }

        private void UpdateBlip()
        {
            if (health > 40)
            {
                step = Math.Max(size.X / 80, size.X / (health));
            }
            else
            {
                step = size.X / 40;
            }
            backgroundSprite.Size = new Vector2D(2, 2);
            backgroundSprite.Opacity = 255;
            blipXRelative += step;
            if (blipXRelative > size.X)
                blipXRelative = 0;
            float blipTemp = 0;
            while (blipTemp <= blipXRelative)
            {
                blipPosition.X = Position.X + blipTemp;
                if (health > 0)
                {
                    if (blipTemp > blipStart + blipWidth || blipTemp < blipStart)
                    {
                        blipPosition.Y = Position.Y + ((size.Y / 3) * 2);
                    }
                    else if (blipTemp < blipStart + blipWidth / 4 || blipTemp > blipStart + ((blipWidth / 4) * 3))
                    {
                        blipPosition.Y--;
                    }
                    else
                    {
                        blipPosition.Y++;
                    }
                }
                backgroundSprite.Position = blipPosition;
                if (blipTemp == blipXRelative)
                    backgroundSprite.Color = Color.DarkBlue;
                else
                    backgroundSprite.Color = Color.Cyan;
                backgroundSprite.Draw();
                blipTemp += step;
            }
        }

        private void DoText()
        {
            healthText.Position = Position;
            if (health > 70)
                healthText.Text = "HEALTHY";
            else if (health > 30)
                healthText.Text = "INJURED";
            else if (health > 0)
                healthText.Text = "CRITICAL!";
            else
                healthText.Text = "DECEASED";
            healthText.Color = Color.Blue;
            healthText.Draw();
        }

        public override void Render()
        {
            backgroundSprite.Color = backgroundColor;
            backgroundSprite.Opacity = 240;
            backgroundSprite.Position = Position;
            backgroundSprite.Size = size;
            backgroundSprite.Draw();
            
            healthDetail.Position = new Vector2D(Position.X, Position.Y);
            healthDetail.Draw();

            DoText();

            UpdateBlip();
        }
    }
}
