using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using ClientServices;
using ClientServices.Resources;
using ClientServices.Configuration;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS13.Modules;
using Lidgren.Network;
using CGO;
using SS13_Shared.GO;
using SS13_Shared;
using SS13.HelperClasses;
using SS13.Modules.Network;


namespace SS13.UserInterface
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

        private Label name;
        private GorgonLibrary.Graphics.Font font;
        private int width = 125;
        private int height = 200;

        private TargetingDummy targetArea;

        private int flickCounter = 0;
        private int noiseStep = 0;
        private bool flick = false;

        private Random rnd = new Random();

        private Sprite flickSprite0;
        private Sprite flickSprite1;

        public StatPanelComponent(PlayerController _playerController, NetworkManager _netMgr)
            : base(_playerController)
        {
            targetArea = new TargetingDummy(_playerController, _netMgr);

            componentClass = SS13_Shared.GuiComponentType.StatPanelComponent;

            Position = new Point(604, Gorgon.Screen.Height - 205);

            font = ResourceManager.GetFont("CALIBRI");
            name = new Label("Name");

            backgroundSprite = ResourceManager.GetSprite("1pxwhite");
            name.Position = new Point(Position.X + 5, Position.Y + 126);
            name.Text.Color = System.Drawing.Color.Green;

            Point size = new Point(114, 36);
            healthComponent = new StatHealthComponent(_playerController, size);
            healthComponent.Position = new Point(Position.X + 5, Position.Y + 158); //Photoshop ruler numbers.

            flickSprite0 = ResourceManager.GetSprite("scanline_statpanel0");
            flickSprite1 = ResourceManager.GetSprite("scanline_statpanel1");

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

            Sprite statPanelBorder = ResourceManager.GetSprite("stat_panel");
            statPanelBorder.Draw(new Rectangle(renderPos.X, renderPos.Y, (int)width, (int)height));

            renderImage.EndDrawing();
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            return targetArea.MouseDown(e);
        }

        public override void Update()
        {
            base.Update();
            targetArea.Update();
        }

        private void DrawPlayer()
        {
            ////TODO RE-CONNECT PLAYER SPRITES TO DISPLAY
            //if (playerSprite == null && playerController.controlledEntity != null)
            //{
            //    playerSprite = Utilities.GetSpriteComponentSprite(playerController.controlledEntity);
            //}

            //playerSprite.UniformScale = 1.5f;
            //playerSprite.Position = new Vector2D(position.X + (width / 2f) - (playerSprite.ScaledWidth / 2f), position.Y + 5);
            //playerSprite.Draw();

            //playerSprite.UniformScale = 1.0f;

            targetArea.Position = new Point(position.X + (int)(width / 2f) - (int)(targetArea.ClientArea.Width / 2f), position.Y + 15);
            targetArea.Render();

            #region noise
            if (flickCounter > 5)
            {
                flick = !flick;
                flickCounter = 0;
            }
            flickCounter++;

            Sprite toDraw = (flick ? flickSprite0 : flickSprite1);
            toDraw.Opacity = 80;
            toDraw.Draw(new Rectangle(new Point(Position.X + 6, Position.Y + 6), new Size((int)toDraw.Width, (int)toDraw.Height)));

            if (rnd.Next(0, 90) == 1) noiseStep = 1;

            if (noiseStep > 0)
            {
                Sprite noise = ResourceManager.GetSprite((noiseStep % 2) == 0 ? "noise_statpanel0" : "noise_statpanel1");
                noiseStep++;
                if (noiseStep == 10) noiseStep = 0;
                noise.Opacity = 40;
                noise.Draw(new Rectangle(new Point(Position.X + 6, Position.Y + 6), new Size((int)noise.Width, (int)noise.Height)));
            } 
            #endregion

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
            if (playerController.ControlledEntity == null)
                return;

            renderImage.Blit(Position.X, Position.Y);

            healthComponent.Render();

            name.Text.Text = ServiceManager.Singleton.GetService<ConfigurationManager>().Configuration.PlayerName;

            name.Update();
            name.Render();

            DrawPlayer();
        }

    }
}
