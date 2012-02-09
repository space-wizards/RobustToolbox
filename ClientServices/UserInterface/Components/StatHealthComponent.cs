using System;
using System.Drawing;
using CGO;
using ClientInterfaces;
using ClientInterfaces.Player;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS13_Shared.GO;

namespace ClientServices.UserInterface.Components
{
    public class StatHealthComponent : GuiComponent
    {
        private readonly IPlayerManager _playerManager;
        private readonly IResourceManager _resourceManager;

        private readonly Sprite _backgroundSprite;
        private readonly Sprite _healthDetailSprite;
        private readonly Label _healthText;
        private readonly float _blipWidth;
        private readonly float _blipStart;

        private float _blipXRelative;
        private float _blipHeight;
        private float _health;
        private float _step;
        private Vector2D _blipPosition;
        private Color _backgroundColor;

        public Point Size;

        public StatHealthComponent(Point size, IPlayerManager playerManager, IResourceManager resourceManager)
        {
            _playerManager = playerManager;
            _resourceManager = resourceManager;

            _backgroundSprite = _resourceManager.GetSprite("1pxwhite");
            _healthDetailSprite = _resourceManager.GetSprite("stat_health_detail");
            _healthText = new Label("Healthy", _resourceManager) {Text = {Color = Color.DarkBlue}};
            Size = size;
            _health = 100;
            SetBackgroundColor();
            _blipPosition = new Vector2D(Position.X, Position.Y + ((Size.Y / 3) * 2));
            _step = Size.X / 100.0f;
            _blipXRelative = 0.0f;
            _blipHeight = Size.Y / 1.2f;
            _blipWidth = Size.X / 4.0f;
            _blipStart = Size.X / 3.0f;
            _healthDetailSprite.Position = new Vector2D(Position.X, Position.Y);
        }

        private void SetBackgroundColor()
        {
            var red = 255 - (int)Math.Round(_health * 2.5f);
            var green = (int)Math.Round(_health * 2.5f);
            const int blue = 50;

            _backgroundColor = Color.FromArgb(red, green, blue);
        }

        private void UpdateBlip()
        {
            if (_health > 40)
            {
                _step = Math.Max(Size.X / 80.0f, Size.X / (_health));
            }
            else
            {
                _step = Size.X / 40.0f;
            }

            _blipXRelative += _step;

            if (_blipXRelative > Size.X)
                _blipXRelative = 0;
            float blipTemp = 0;

            while (blipTemp <= _blipXRelative)
            {
                _blipPosition.X = Position.X + blipTemp;

                if (_health > 0)
                {
                    if (blipTemp > _blipStart + _blipWidth || blipTemp < _blipStart)
                    {
                        _blipPosition.Y = Position.Y + ((Size.Y / 3) * 2);
                    }
                    else if (blipTemp < _blipStart + _blipWidth / 4 || blipTemp > _blipStart + ((_blipWidth / 4) * 3))
                    {
                        _blipPosition.Y--;
                    }
                    else
                    {
                        _blipPosition.Y++;
                    }
                }

                _backgroundSprite.Color = Equals(blipTemp, _blipXRelative) ? Color.White : Color.Cyan;

                _backgroundSprite.Opacity = 130;
                _backgroundSprite.Draw(new Rectangle((int)_blipPosition.X, (int)_blipPosition.Y, 2, 2));

                blipTemp += _step;
            }
        }

        public override void Update()
        {

        }

        private void DoText()
        {
            var entity = _playerManager.ControlledEntity;

            _healthText.Text.Text = "???";

            if (entity.HasComponent(ComponentFamily.Damageable))
            {
                var comp = (HealthComponent)entity.GetComponent(ComponentFamily.Damageable);
                _health = comp.GetHealth();

                var healthPct = comp.GetHealth() / comp.GetMaxHealth();

                if (healthPct > 0.75) _healthText.Text.Text = "HEALTHY: " + _health;
                else if (healthPct > 0.50) _healthText.Text.Text = "INJURED: " + _health;
                else if (healthPct > 0.25) _healthText.Text.Text = "WOUNDED: " + _health;
                else if (healthPct > 0) _healthText.Text.Text = "CRITICAL: " + _health;
                else _healthText.Text.Text = "DECEASED: " + _health;
            }

            _healthText.Position = Position;

            _healthText.Text.Color = Color.Blue;

            _healthText.Update();
            _healthText.Render();
        }

        public override void Render()
        {
            _backgroundSprite.Color = _backgroundColor;
            _backgroundSprite.Opacity = 125;

            _backgroundSprite.Draw(new Rectangle(Position, new Size(Size.X, Size.Y) ) );
            
            _healthDetailSprite.Position = new Vector2D(Position.X, Position.Y);
            _healthDetailSprite.Draw();

            DoText();

            UpdateBlip();
        }
    }
}
