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
using ClientConfigManager;
using ClientResourceManager;

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

        private RenderImage renderImage;

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
            componentClass = SS3D_shared.GuiComponentType.StatPanelComponent;
            Position = new Point(604, Gorgon.Screen.Height - 205);

            font = ResMgr.Singleton.GetFont("CALIBRI");
            name = new TextSprite("statpanelname", "Name", font);
            Skin = UIDesktop.Singleton.Skin;
            backgroundSprite = ResMgr.Singleton.GetSprite("1pxwhite");
            name.Position = new Vector2D(Position.X + ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.LeftCorner").Width + 2, Position.Y + (height / 3 * 2) + 2);
            name.Color = System.Drawing.Color.Green;

            Point size = new Point(width - ResMgr.Singleton.GetGUIInfo("Window.Border.Vertical.Left").Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Vertical.Right").Width, (height / 5) - ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.Horizontal").Height);
            healthComponent = new StatHealthComponent(_playerController, size);
            healthComponent.Position = new Point(Position.X + ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.LeftCorner").Width, Position.Y + (height / 5 * 4));

            renderImage = new RenderImage("statpanelRI", width, height, ImageBufferFormats.BufferUnknown);
            renderImage.ClearEachFrame = ClearTargets.None;
            PreRender();
        }

        private void PreRender()
        {
            Point renderPos = new Point(0, 0);

            renderImage.BeginDrawing();
            backgroundSprite.Color = System.Drawing.Color.FromArgb(51, 56, 64);
            backgroundSprite.Opacity = 240;
            backgroundSprite.Position = renderPos;
            backgroundSprite.Size = new Vector2D(width, height);
            backgroundSprite.Draw();

            
            Skin.Elements["Window.Border.Top.LeftCorner"].Draw(new System.Drawing.Rectangle(renderPos.X, renderPos.Y, ResMgr.Singleton.GetGUIInfo("Window.Border.Top.LeftCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Top.LeftCorner").Height));
            Skin.Elements["Window.Border.Top.Horizontal"].Draw(new System.Drawing.Rectangle(renderPos.X + ResMgr.Singleton.GetGUIInfo("Window.Border.Top.LeftCorner").Width, renderPos.Y, width - ResMgr.Singleton.GetGUIInfo("Window.Border.Top.RightCorner").Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Top.RightCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Top.Horizontal").Height));
            Skin.Elements["Window.Border.Top.RightCorner"].Draw(new System.Drawing.Rectangle(renderPos.X + width - ResMgr.Singleton.GetGUIInfo("Window.Border.Top.RightCorner").Width, renderPos.Y, ResMgr.Singleton.GetGUIInfo("Window.Border.Top.RightCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Top.RightCorner").Height));

            Skin.Elements["Window.Border.Vertical.Left"].Draw(new System.Drawing.Rectangle(renderPos.X, ResMgr.Singleton.GetGUIInfo("Window.Border.Top.LeftCorner").Height + renderPos.Y, ResMgr.Singleton.GetGUIInfo("Window.Border.Vertical.Left").Width, height - ResMgr.Singleton.GetGUIInfo("Window.Border.Top.LeftCorner").Height - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.LeftCorner").Height));
            Skin.Elements["Window.Border.Vertical.Right"].Draw(new System.Drawing.Rectangle(renderPos.X + width - ResMgr.Singleton.GetGUIInfo("Window.Border.Vertical.Right").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Top.Horizontal").Height + renderPos.Y, ResMgr.Singleton.GetGUIInfo("Window.Border.Vertical.Right").Width, height - ResMgr.Singleton.GetGUIInfo("Window.Border.Top.RightCorner").Height - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.RightCorner").Height));

            Skin.Elements["Window.Border.Middle.LeftCorner"].Draw(new System.Drawing.Rectangle(renderPos.X, renderPos.Y + (height / 3 * 2) - ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.LeftCorner").Height, ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.LeftCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.LeftCorner").Height));
            Skin.Elements["Window.Border.Middle.Horizontal"].Draw(new System.Drawing.Rectangle(renderPos.X + ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.LeftCorner").Width, renderPos.Y + (height / 3 * 2) - ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.Horizontal").Height, width - ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.RightCorner").Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.LeftCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.Horizontal").Height));
            Skin.Elements["Window.Border.Middle.RightCorner"].Draw(new System.Drawing.Rectangle(renderPos.X + width - ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.RightCorner").Width, renderPos.Y + (height / 3 * 2) - ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.RightCorner").Height, ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.RightCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.RightCorner").Height));

            Skin.Elements["Window.Border.Middle.LeftCorner"].Draw(new System.Drawing.Rectangle(renderPos.X, renderPos.Y + (height / 5 * 4) - ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.LeftCorner").Height, ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.LeftCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.LeftCorner").Height));
            Skin.Elements["Window.Border.Middle.Horizontal"].Draw(new System.Drawing.Rectangle(renderPos.X + ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.LeftCorner").Width, renderPos.Y + (height / 5 * 4) - ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.Horizontal").Height, width - ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.RightCorner").Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.LeftCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.Horizontal").Height));
            Skin.Elements["Window.Border.Middle.RightCorner"].Draw(new System.Drawing.Rectangle(renderPos.X + width - ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.RightCorner").Width, renderPos.Y + (height / 5 * 4) - ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.RightCorner").Height, ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.RightCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.RightCorner").Height));

            Skin.Elements["Window.Border.Bottom.LeftCorner"].Draw(new System.Drawing.Rectangle(renderPos.X, renderPos.Y + height - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.LeftCorner").Height, ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.LeftCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.LeftCorner").Height));
            Skin.Elements["Window.Border.Bottom.Horizontal"].Draw(new System.Drawing.Rectangle(renderPos.X + ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.LeftCorner").Width, renderPos.Y + height - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.Horizontal").Height, width - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.RightCorner").Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.LeftCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.Horizontal").Height));
            Skin.Elements["Window.Border.Bottom.RightCorner"].Draw(new System.Drawing.Rectangle(renderPos.X + width - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.RightCorner").Width, renderPos.Y + height - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.RightCorner").Height, ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.RightCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.RightCorner").Height));
            renderImage.EndDrawing();

        }

        private void DrawPlayer()
        {
           if (playerSprite == null && playerController.controlledAtom != null)
            {
                playerSprite = playerController.controlledAtom.sprite;
            }

            playerSprite.Position = new Vector2D(position.X + width / 2, position.Y + height / 3.1f);
            playerSprite.UniformScale = 1.5f;
            //playerSprite.ImageRegion = new RectangleF(0, 0, 32, 20);
            playerSprite.Draw();

            playerSprite.UniformScale = 1.0f;

            bool scanSwitch = false;
            if (flickCounter > 5)
            {
                Random r = new Random();
                if (r.NextDouble() > 0.8)
                    scanSwitch = true;
                flickCounter = 0;
            }
            flickCounter++;
            backgroundSprite.Position = new Vector2D(Position.X + ResMgr.Singleton.GetGUIInfo("Window.Border.Vertical.Left").Width, Position.Y + ResMgr.Singleton.GetGUIInfo("Window.Border.Top.Horizontal").Height - 2);
            backgroundSprite.Size = new Vector2D(width - ResMgr.Singleton.GetGUIInfo("Window.Border.Vertical.Left").Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Vertical.Right").Width, 2);
            for (int i = 0; i < ((height / 3 * 2) - ResMgr.Singleton.GetGUIInfo("Window.Border.Top.Horizontal").Height - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.Horizontal").Height) / 2; i++)
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
            GuiComponentType statPanelSubtype = (GuiComponentType)message.ReadByte();
            switch (statPanelSubtype)
            {
                case GuiComponentType.HealthComponent:
                    healthComponent.HandleNetworkMessage(message);
                    break;
            }
        }

        public override void Render()
        {
            if (Skin == null || playerController.controlledAtom == null)
                return;

            renderImage.Blit(Position.X, Position.Y);

            healthComponent.Render();
            name.Text = ConfigManager.Singleton.Configuration.PlayerName; // Name isn't currently set so this is just temporary
            name.Draw();

            DrawPlayer();
                   
             
        }



    }
}
