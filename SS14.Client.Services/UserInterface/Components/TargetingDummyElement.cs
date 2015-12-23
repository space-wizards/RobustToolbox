using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using System;
using System.Drawing;
using SS14.Client.Graphics.Sprite;
using SFML.Window;
using Image = SFML.Graphics.Image;
using SS14.Client.Graphics;
using SS14.Shared.Maths;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class TargetingDummyElement : GuiComponent
    {
        #region Delegates

        public delegate void TargetingDummyElementPressHandler(TargetingDummyElement sender);

        #endregion

        private readonly IResourceManager _resourceManager;
        public BodyPart BodyPart;
        public float CurrentHealth;
        public float MaxHealth;
        private Point _clickPoint;

        private CluwneSprite _elementSprite;
        private Boolean _selected;

        public TargetingDummyElement(string spriteName, BodyPart part, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            BodyPart = part;
            _elementSprite = _resourceManager.GetSprite(spriteName);
            Update(0);
        }

        public event TargetingDummyElementPressHandler Clicked;

        public void Select()
        {
            _selected = true;
        }

        public bool IsSelected()
        {
            return _selected;
        }

        public void ClearSelected()
        {
            _selected = false;
        }

        public override sealed void Update(float frameTime)
        {
            _elementSprite.Position = new Vector2(Position.X,Position.Y);
            ClientArea = new Rectangle(Position,
                                       new Size((int)_elementSprite.Width, (int)_elementSprite.Height));
        }

        public override void Render()
        {
            //elementSprite.Color = selected ? Color.DarkRed : Color.White;
            float healthPct = CurrentHealth / MaxHealth;

            if (healthPct > 0.75) _elementSprite.Color      = Color.DarkGreen;
            else if (healthPct > 0.50) _elementSprite.Color = Color.Yellow;
            else if (healthPct > 0.25) _elementSprite.Color = Color.DarkOrange;
            else if (healthPct > 0) _elementSprite.Color    = Color.Red;
            else _elementSprite.Color = Color.Black;

            _elementSprite.Position = new Vector2(Position.X,Position.Y);
            _elementSprite.Draw();
            _elementSprite.Color = Color.White;

            if (!_selected) return;

           CluwneLib.drawCircle(Position.X + _clickPoint.X, Position.Y + _clickPoint.Y, 5, Color.Black);
           CluwneLib.drawCircle(Position.X + _clickPoint.X, Position.Y + _clickPoint.Y, 4, Color.DarkRed);
           CluwneLib.drawCircle(Position.X + _clickPoint.X, Position.Y + _clickPoint.Y, 3, Color.Black);
        }

        public override void Dispose()
        {
            _elementSprite = null;
            Clicked = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (!ClientArea.Contains(new Point((int)e.X, (int)e.Y))) return false;

            var spritePosition = new Point((int)e.X - Position.X + (int)_elementSprite.ImageOffset.X,
                                           (int)e.Y - Position.Y + (int)_elementSprite.ImageOffset.Y);

            // Image.ImageLockBox imgData = _elementSprite.Image.GetImageData();
            //imgData.Lock(false);

            Color pixColour = Color.Red;
            //imgData.Dispose();
            //imgData.Unlock();

            if (pixColour.A != 0)
            {
                if (Clicked != null) Clicked(this);
                _clickPoint = new Point((int)e.X - Position.X, (int)e.Y - Position.Y);
                _selected = true;
                return true;
            }

            _selected = false;
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            return false;
        }
    }
}