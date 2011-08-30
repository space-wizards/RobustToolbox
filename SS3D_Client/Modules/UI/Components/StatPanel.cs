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
    public class StatPanelComponent : GuiComponent
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

        private StatHealthComponent healthComponent;
        private Sprite backgroundSprite;
        private Sprite playerSprite;
        private TextSprite name;
        private GorgonLibrary.Graphics.Font font;
        private GorgonLibrary.GUI.GUISkin Skin;
        private int width = 125;
        private int height = 200;
        private int flickCounter = 0;

        public StatPanelComponent(PlayerController _playerController)
            : base(_playerController)
        {
            Position = new Point(604, Gorgon.Screen.Height - 205);

            font = GorgonLibrary.Graphics.Font.FromFile(@"..\..\..\Media\Fonts\\CALIBRI.TTF", 10);
            name = new TextSprite("statpanelname", "Name", font);
            Skin = UIDesktop.Singleton.Skin;
            backgroundSprite = ResMgr.Singleton.GetSprite("1pxwhite");
            backgroundSprite.Color = System.Drawing.Color.FromArgb(51, 56, 64);
            backgroundSprite.Opacity = 240;
            backgroundSprite.Position = Position;
            backgroundSprite.Size = new Vector2D(width, height);

            name.Position = new Vector2D(Position.X + Skin.Elements["Window.Border.Middle.LeftCorner"].Dimensions.Width + 2, Position.Y + (height / 3 * 2) + 2);
            name.Color = System.Drawing.Color.Green;

            Point size = new Point(width - Skin.Elements["Window.Border.Vertical.Left"].Dimensions.Width - Skin.Elements["Window.Border.Vertical.Right"].Dimensions.Width, (height / 5) - Skin.Elements["Window.Border.Middle.Horizontal"].Dimensions.Height);
            healthComponent = new StatHealthComponent(_playerController, size);
            healthComponent.Position = new Point(Position.X + Skin.Elements["Window.Border.Middle.LeftCorner"].Dimensions.Width, Position.Y + (height / 5 * 4));
        }

        private void DrawPlayer()
        {
           if (playerSprite == null)
            {
                playerSprite = new Sprite("statplayersprite", playerController.controlledAtom.sprite.Image);
            }

            playerSprite.Position = new Vector2D(Position.X + 5, Position.Y + 35);
            playerSprite.UniformScale = 4.5f;
            playerSprite.ImageRegion = new RectangleF(0, 0, 26, 21);
            playerSprite.Draw();

            bool scanSwitch = false;
            if (flickCounter > 5)
            {
                Random r = new Random();
                if (r.NextDouble() > 0.8)
                    scanSwitch = true;
                flickCounter = 0;
            }
            flickCounter++;
            backgroundSprite.Position = new Vector2D(Position.X + Skin.Elements["Window.Border.Vertical.Left"].Dimensions.Width, Position.Y + Skin.Elements["Window.Border.Top.Horizontal"].Dimensions.Height - 2);
            backgroundSprite.Size = new Vector2D(width - Skin.Elements["Window.Border.Vertical.Left"].Dimensions.Width - Skin.Elements["Window.Border.Vertical.Right"].Dimensions.Width, 2);
            for (int i = 0; i < ((height / 3 * 2) - Skin.Elements["Window.Border.Top.Horizontal"].Dimensions.Height - Skin.Elements["Window.Border.Bottom.Horizontal"].Dimensions.Height) / 2; i++)
            {
                if (i % 2 == 0)
                {
                    if (scanSwitch)
                        backgroundSprite.Color = System.Drawing.Color.White;
                    else
                        backgroundSprite.Color = System.Drawing.Color.DarkGray;
                }
                else
                {
                    if (scanSwitch)
                        backgroundSprite.Color = System.Drawing.Color.DarkGray;
                    else
                        backgroundSprite.Color = System.Drawing.Color.White;
                }
                backgroundSprite.Position += new Vector2D(0, 2);
                backgroundSprite.Opacity = 50;
                backgroundSprite.Draw();
            }

        }

        public override void HandleNetworkMessage(NetIncomingMessage message)
        {
            healthComponent.HandleNetworkMessage(message);
        }

        public override void Render()
        {
            if (Skin == null || playerController.controlledAtom == null)
                return;

            backgroundSprite.Color = System.Drawing.Color.FromArgb(51, 56, 64);
            backgroundSprite.Opacity = 240;
            backgroundSprite.Position = Position;
            backgroundSprite.Size = new Vector2D(width, height);
            backgroundSprite.Draw();

            name.Text = playerController.controlledAtom.spritename; // Name isn't currently set so this is just temporary
            name.Draw();

            DrawPlayer();
            healthComponent.Render();

            Skin.Elements["Window.Border.Top.LeftCorner"].Draw(new System.Drawing.Rectangle(Position.X, Position.Y, Skin.Elements["Window.Border.Top.LeftCorner"].Dimensions.Width, Skin.Elements["Window.Border.Top.LeftCorner"].Dimensions.Height));
            Skin.Elements["Window.Border.Top.Horizontal"].Draw(new System.Drawing.Rectangle(Position.X + Skin.Elements["Window.Border.Top.LeftCorner"].Dimensions.Width, Position.Y, width - Skin.Elements["Window.Border.Top.RightCorner"].Dimensions.Width - Skin.Elements["Window.Border.Top.RightCorner"].Dimensions.Width, Skin.Elements["Window.Border.Top.Horizontal"].Dimensions.Height));
            Skin.Elements["Window.Border.Top.RightCorner"].Draw(new System.Drawing.Rectangle(Position.X + width - Skin.Elements["Window.Border.Top.RightCorner"].Dimensions.Width, Position.Y, Skin.Elements["Window.Border.Top.RightCorner"].Dimensions.Width, Skin.Elements["Window.Border.Top.RightCorner"].Dimensions.Height));

            Skin.Elements["Window.Border.Vertical.Left"].Draw(new System.Drawing.Rectangle(Position.X, Skin.Elements["Window.Border.Top.LeftCorner"].Dimensions.Height + Position.Y, Skin.Elements["Window.Border.Vertical.Left"].Dimensions.Width, height - Skin.Elements["Window.Border.Top.LeftCorner"].Dimensions.Height - Skin.Elements["Window.Border.Bottom.LeftCorner"].Dimensions.Height));
            Skin.Elements["Window.Border.Vertical.Right"].Draw(new System.Drawing.Rectangle(Position.X + width - Skin.Elements["Window.Border.Vertical.Right"].Dimensions.Width, Skin.Elements["Window.Border.Top.Horizontal"].Dimensions.Height + Position.Y, Skin.Elements["Window.Border.Vertical.Right"].Dimensions.Width, height - Skin.Elements["Window.Border.Top.RightCorner"].Dimensions.Height - Skin.Elements["Window.Border.Bottom.RightCorner"].Dimensions.Height));

            Skin.Elements["Window.Border.Middle.LeftCorner"].Draw(new System.Drawing.Rectangle(Position.X, Position.Y + (height / 3 * 2) - Skin.Elements["Window.Border.Middle.LeftCorner"].Dimensions.Height, Skin.Elements["Window.Border.Middle.LeftCorner"].Dimensions.Width, Skin.Elements["Window.Border.Middle.LeftCorner"].Dimensions.Height));
            Skin.Elements["Window.Border.Middle.Horizontal"].Draw(new System.Drawing.Rectangle(Position.X + Skin.Elements["Window.Border.Middle.LeftCorner"].Dimensions.Width, Position.Y + (height / 3 * 2)- Skin.Elements["Window.Border.Middle.Horizontal"].Dimensions.Height, width - Skin.Elements["Window.Border.Middle.RightCorner"].Dimensions.Width - Skin.Elements["Window.Border.Middle.LeftCorner"].Dimensions.Width, Skin.Elements["Window.Border.Middle.Horizontal"].Dimensions.Height));
            Skin.Elements["Window.Border.Middle.RightCorner"].Draw(new System.Drawing.Rectangle(Position.X + width - Skin.Elements["Window.Border.Middle.RightCorner"].Dimensions.Width, Position.Y + (height / 3 * 2) - Skin.Elements["Window.Border.Middle.RightCorner"].Dimensions.Height, Skin.Elements["Window.Border.Middle.RightCorner"].Dimensions.Width, Skin.Elements["Window.Border.Middle.RightCorner"].Dimensions.Height));

            Skin.Elements["Window.Border.Middle.LeftCorner"].Draw(new System.Drawing.Rectangle(Position.X, Position.Y + (height / 5 * 4) - Skin.Elements["Window.Border.Middle.LeftCorner"].Dimensions.Height, Skin.Elements["Window.Border.Middle.LeftCorner"].Dimensions.Width, Skin.Elements["Window.Border.Middle.LeftCorner"].Dimensions.Height));
            Skin.Elements["Window.Border.Middle.Horizontal"].Draw(new System.Drawing.Rectangle(Position.X + Skin.Elements["Window.Border.Middle.LeftCorner"].Dimensions.Width, Position.Y + (height / 5 * 4) - Skin.Elements["Window.Border.Middle.Horizontal"].Dimensions.Height, width - Skin.Elements["Window.Border.Middle.RightCorner"].Dimensions.Width - Skin.Elements["Window.Border.Middle.LeftCorner"].Dimensions.Width, Skin.Elements["Window.Border.Middle.Horizontal"].Dimensions.Height));
            Skin.Elements["Window.Border.Middle.RightCorner"].Draw(new System.Drawing.Rectangle(Position.X + width - Skin.Elements["Window.Border.Middle.RightCorner"].Dimensions.Width, Position.Y + (height / 5 * 4) - Skin.Elements["Window.Border.Middle.RightCorner"].Dimensions.Height, Skin.Elements["Window.Border.Middle.RightCorner"].Dimensions.Width, Skin.Elements["Window.Border.Middle.RightCorner"].Dimensions.Height));

            Skin.Elements["Window.Border.Bottom.LeftCorner"].Draw(new System.Drawing.Rectangle(Position.X, Position.Y + height - Skin.Elements["Window.Border.Bottom.LeftCorner"].Dimensions.Height, Skin.Elements["Window.Border.Bottom.LeftCorner"].Dimensions.Width, Skin.Elements["Window.Border.Bottom.LeftCorner"].Dimensions.Height));
            Skin.Elements["Window.Border.Bottom.Horizontal"].Draw(new System.Drawing.Rectangle(Position.X + Skin.Elements["Window.Border.Bottom.LeftCorner"].Dimensions.Width, Position.Y + height - Skin.Elements["Window.Border.Bottom.Horizontal"].Dimensions.Height, width - Skin.Elements["Window.Border.Bottom.RightCorner"].Dimensions.Width - Skin.Elements["Window.Border.Bottom.LeftCorner"].Dimensions.Width, Skin.Elements["Window.Border.Bottom.Horizontal"].Dimensions.Height));
            Skin.Elements["Window.Border.Bottom.RightCorner"].Draw(new System.Drawing.Rectangle(position.X + width - Skin.Elements["Window.Border.Bottom.RightCorner"].Dimensions.Width, Position.Y + height - Skin.Elements["Window.Border.Bottom.RightCorner"].Dimensions.Height, Skin.Elements["Window.Border.Bottom.RightCorner"].Dimensions.Width, Skin.Elements["Window.Border.Bottom.RightCorner"].Dimensions.Height));
        }



    }
}
