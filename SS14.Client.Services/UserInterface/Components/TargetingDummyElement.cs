using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using System;

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
        private Vector2i _clickPoint;

        private Sprite _elementSprite;
        private bool _selected;

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
            _elementSprite.Position = new Vector2f(Position.X,Position.Y);
            var bounds = _elementSprite.GetLocalBounds();
            ClientArea = new IntRect(Position,
                                       new Vector2i((int)bounds.Width, (int)bounds.Height));
        }

        public override void Render()
        {
            //elementSprite.Color = selected ? Color.DarkRed : Color.White;
            float healthPct = CurrentHealth / MaxHealth;

            if (healthPct > 0.75) _elementSprite.Color      = new Color(0, 128, 0);
            else if (healthPct > 0.50) _elementSprite.Color = Color.Yellow;
            else if (healthPct > 0.25) _elementSprite.Color = new Color(128, 64, 0);
            else if (healthPct > 0) _elementSprite.Color    = Color.Red;
            else _elementSprite.Color = Color.Black;

            _elementSprite.Position = new Vector2f(Position.X,Position.Y);
            _elementSprite.Draw();
            _elementSprite.Color = Color.White;

            if (!_selected) return;

           CluwneLib.drawCircle(Position.X + _clickPoint.X, Position.Y + _clickPoint.Y, 5, Color.Black);
           CluwneLib.drawCircle(Position.X + _clickPoint.X, Position.Y + _clickPoint.Y, 4, new Color(139, 0, 0));
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
            if (!ClientArea.Contains(e.X, e.Y)) return false;

            // Image.ImageLockBox imgData = _elementSprite.Image.GetImageData();
            //imgData.Lock(false);

            Color pixColour = Color.Red;
            //imgData.Dispose();
            //imgData.Unlock();

            if (pixColour.A != 0)
            {
                if (Clicked != null) Clicked(this);
                _clickPoint = new Vector2i(e.X - Position.X, e.Y - Position.Y);
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