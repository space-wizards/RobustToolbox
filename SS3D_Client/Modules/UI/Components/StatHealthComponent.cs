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
            backgroundSprite = ResMgr.Singleton.GetSprite("1pxwhite");
            healthDetail = ResMgr.Singleton.GetSprite("stat_health_detail");
            size = _size;
            health = 100;
            SetBackgroundColor();
            blipPosition = new Vector2D(Position.X, Position.Y + (size.Y / 2));
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
            int blue = 0;

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
                step = size.X / 30;
            }
            backgroundSprite.Size = new Vector2D(2, 2);
            backgroundSprite.Opacity = 255;
            blipXRelative += step;
            if (blipXRelative > size.X)
                blipXRelative = 0;

            blipPosition.X = Position.X + blipXRelative;
            if (health > 0)
            {
                if (blipXRelative > blipStart + blipWidth || blipXRelative < blipStart)
                {
                    blipPosition.Y = Position.Y + (size.Y / 2);
                }
                else if (blipXRelative < blipStart + blipWidth / 4 || blipXRelative > blipStart + ((blipWidth / 4) * 3))
                {
                    blipPosition.Y--;
                }
                else
                {
                    blipPosition.Y++;
                }
            }



            backgroundSprite.Position = blipPosition;
            backgroundSprite.Color = Color.Blue;


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

            UpdateBlip();
            backgroundSprite.Draw();
        }
    }
}
