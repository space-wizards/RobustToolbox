using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS3D.Modules;
using Lidgren.Network;
using CGO;
using SS3D_shared.GO;
using SS3D_shared;
using ClientConfigManager;
using ClientResourceManager;
using SS3D.HelperClasses;

namespace SS3D.UserInterface
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
        private Label name;
        private GorgonLibrary.Graphics.Font font;
        private int width = 125;
        private int height = 200;

        private int flickCounter = 0;
        private bool flick = false;

        private Sprite flickSprite0;
        private Sprite flickSprite1;

        public StatPanelComponent(PlayerController _playerController)
            : base(_playerController)
        {
            componentClass = SS3D_shared.GuiComponentType.StatPanelComponent;

            Position = new Point(604, Gorgon.Screen.Height - 205);

            font = ResMgr.Singleton.GetFont("CALIBRI");
            name = new Label("Name");

            backgroundSprite = ResMgr.Singleton.GetSprite("1pxwhite");
            name.Position = new Point(Position.X + 5, Position.Y + 126);
            name.Text.Color = System.Drawing.Color.Green;

            Point size = new Point(114, 36);
            healthComponent = new StatHealthComponent(_playerController, size);
            healthComponent.Position = new Point(Position.X + 5, Position.Y + 158); //Photoshop ruler numbers.

            flickSprite0 = ResMgr.Singleton.GetSprite("scanline_statpanel0");
            flickSprite1 = ResMgr.Singleton.GetSprite("scanline_statpanel1");

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
            backgroundSprite.Draw(new Rectangle(renderPos, new Size(width, height)));

            Sprite statPanelBorder = ResMgr.Singleton.GetSprite("stat_panel");
            statPanelBorder.Draw(new Rectangle(renderPos.X, renderPos.Y, (int)width, (int)height));

            renderImage.EndDrawing();

        }

        private void DrawPlayer()
        {
            //TODO RE-CONNECT PLAYER SPRITES TO DISPLAY
            if (playerSprite == null && playerController.controlledAtom != null)
            {
                playerSprite = Utilities.GetSpriteComponentSprite(playerController.controlledAtom);
            }

            playerSprite.UniformScale = 1.5f;
            playerSprite.Position = new Vector2D(position.X + (width / 2f) - (playerSprite.ScaledWidth / 2f), position.Y + 5);
            playerSprite.Draw();

            playerSprite.UniformScale = 1.0f;

            if (flickCounter > 5)
            {
                flick = !flick;
                flickCounter = 0;
            }
            flickCounter++;
    
            Sprite toDraw = (flick ? flickSprite1 : flickSprite0);
            toDraw.Opacity = 160;
            toDraw.Draw(new Rectangle(new Point(Position.X + 5, Position.Y + 5), new Size((int)toDraw.Width, (int)toDraw.Height)));
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
            if (playerController.controlledAtom == null)
                return;

            renderImage.Blit(Position.X, Position.Y);

            healthComponent.Render();

            name.Text.Text = ConfigManager.Singleton.Configuration.PlayerName;

            name.Update();
            name.Render();

            DrawPlayer();
        }

    }
}
