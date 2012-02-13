using System;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Network;
using ClientInterfaces.Player;
using ClientInterfaces.Resource;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using Lidgren.Network;
using SS13_Shared;

namespace ClientServices.UserInterface.Components
{
    public class StatPanelComponent : GuiComponent
    {
        private const int Width = 125;
        private const int Height = 200;

        private readonly IPlayerManager _playerManager;
        private readonly INetworkManager _networkManager;
        private readonly IResourceManager _resourceManager;
        private readonly StatHealthComponent _healthComponent;
        private readonly TargetingDummy _targetArea;
        private readonly RenderImage _renderImage;
        private readonly Sprite _backgroundSprite;
        private readonly Sprite _flickSprite0;
        private readonly Sprite _flickSprite1;
        private readonly Random _random;
        private readonly Label _name;

        private int _flickCounter;
        private int _noiseStep;
        private bool _flick;

        public StatPanelComponent(string playerName, IPlayerManager playerManager, INetworkManager networkManager, IResourceManager resourceManager)
        {
            _playerManager = playerManager;
            _networkManager = networkManager;
            _resourceManager = resourceManager;

            _random = new Random();

            _targetArea = new TargetingDummy(_playerManager, _networkManager, _resourceManager);

            ComponentClass = GuiComponentType.StatPanelComponent;

            Position = new Point(604, Gorgon.CurrentRenderTarget.Height - 205);

            _name = new Label("Name", "CALIBRI", _resourceManager) { Text = { Text = playerName } };

            _backgroundSprite = _resourceManager.GetSprite("1pxwhite");
            _name.Position = new Point(Position.X + 5, Position.Y + 126);
            _name.Text.Color = Color.Green;

            var size = new Point(114, 36);
            _healthComponent = new StatHealthComponent(size, _playerManager, _resourceManager)
                                   {
                                       Position = new Point(Position.X + 5, Position.Y + 158) //Photoshop ruler numbers.
                                   };

            _flickSprite0 = _resourceManager.GetSprite("scanline_statpanel0");
            _flickSprite1 = _resourceManager.GetSprite("scanline_statpanel1");

            _renderImage = new RenderImage("statpanelRI", Width, Height, ImageBufferFormats.BufferUnknown)
                               {ClearEachFrame = ClearTargets.None};
            PreRender();
        }

        private void PreRender()
        {
            var renderPos = new Point(0, 0);

            _renderImage.BeginDrawing();
            _backgroundSprite.Color = Color.FromArgb(51, 56, 64);
            _backgroundSprite.Opacity = 240;
            _backgroundSprite.Draw(new Rectangle(renderPos, new Size(Width, Height)));

            Sprite statPanelBorder = _resourceManager.GetSprite("stat_panel");
            statPanelBorder.Draw(new Rectangle(renderPos.X, renderPos.Y, Width, Height));

            _renderImage.EndDrawing();
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            return _targetArea.MouseDown(e);
        }

        public override void Update()
        {
            base.Update();
            _targetArea.Update();
        }

        public override void Resize()
        {
            PreRender();
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

            _targetArea.Position = new Point(Position.X + (int)(Width / 2f) - (int)(_targetArea.ClientArea.Width / 2f), Position.Y + 15);
            _targetArea.Render();

            #region Noise
            if (_flickCounter > 5)
            {
                _flick = !_flick;
                _flickCounter = 0;
            }
            _flickCounter++;

            Sprite toDraw = (_flick ? _flickSprite0 : _flickSprite1);
            toDraw.Opacity = 80;
            toDraw.Draw(new Rectangle(new Point(Position.X + 6, Position.Y + 6), new Size((int)toDraw.Width, (int)toDraw.Height)));

            if (_random.Next(0, 90) == 1) _noiseStep = 1;

            if (_noiseStep > 0)
            {
                Sprite noise = _resourceManager.GetSprite((_noiseStep % 2) == 0 ? "noise_statpanel0" : "noise_statpanel1");
                _noiseStep++;
                if (_noiseStep == 10) _noiseStep = 0;
                noise.Opacity = 40;
                noise.Draw(new Rectangle(new Point(Position.X + 6, Position.Y + 6), new Size((int)noise.Width, (int)noise.Height)));
            } 
            #endregion

        }

        public override void HandleNetworkMessage(NetIncomingMessage message)
        {
            var statPanelSubtype = (GuiComponentType)message.ReadByte();
            switch (statPanelSubtype)
            {
                case GuiComponentType.HealthComponent:
                    _healthComponent.HandleNetworkMessage(message);
                    break;
            }
        }

        public override void Render()
        {
            if (_playerManager.ControlledEntity == null)
                return;

            _renderImage.Blit(Position.X, Position.Y);

            _healthComponent.Render();

            _name.Update();
            _name.Render();

            DrawPlayer();
        }

    }
}
