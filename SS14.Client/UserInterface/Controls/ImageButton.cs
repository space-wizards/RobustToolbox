using SS14.Client.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Sprites;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    public class ImageButton : Control
    {
        public delegate void ImageButtonPressHandler(ImageButton sender);

        private Sprite _buttonClick;
        private Sprite _buttonHover;
        private Sprite _buttonNormal;

        private Sprite _drawSprite;

        public string ImageNormal
        {
            set
            {
                _buttonNormal = new Sprite(_resourceCache.GetSprite(value));

                if (_drawSprite == null)
                    _drawSprite = _buttonNormal;
            }
        }

        public string ImageHover
        {
            set => _buttonHover = new Sprite(_resourceCache.GetSprite(value));
        }

        public string ImageClick
        {
            set => _buttonClick = new Sprite(_resourceCache.GetSprite(value));
        }

        public override Color ForegroundColor
        {
            set
            {
                base.ForegroundColor = value;
                _buttonNormal.Color = value;
                if (_buttonClick != null)
                {
                    _buttonClick.Color = value;
                }
                if (_buttonHover != null)
                {
                    _buttonHover.Color = value;
                }
            }
        }

        /// <inheritdoc />
        protected override void OnCalcRect()
        {
            if (_buttonNormal != null)
            {
                var bounds = _drawSprite.LocalBounds;
                _size = new Vector2i((int)bounds.Width, (int)bounds.Height);
                _clientArea = new Box2i(0, 0, (int)bounds.Width, (int)bounds.Height);
            }
        }

        /// <inheritdoc />
        protected override void DrawContents()
        {
            if (_drawSprite == null)
                return;

            _drawSprite.Position = new Vector2(Position.X, Position.Y); // mouse events swap _drawSprite at any time, need to be kept in sync here
            _drawSprite.Draw(CluwneLib.CurrentRenderTarget, new RenderStates(BlendMode.Alpha));
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            _buttonNormal = null;
            _buttonHover = null;
            _buttonClick = null;
            Clicked = null;

            base.Dispose();
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            if (ClientArea.Translated(_screenPos).Contains(e.X, e.Y) && _buttonHover != null)
            {
                if (_drawSprite != _buttonClick)
                    _drawSprite = _buttonHover;
            }
            else
            {
                if (_drawSprite != _buttonClick)
                    _drawSprite = _buttonNormal;
            }

            base.MouseMove(e);
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Translated(_screenPos).Contains(e.X, e.Y))
            {
                if (_buttonClick != null) _drawSprite = _buttonClick;
                Clicked?.Invoke(this);
                return true;
            }

            return base.MouseDown(e);
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (_drawSprite == _buttonClick)
                if (_buttonHover != null)
                    _drawSprite = ClientArea.Translated(_screenPos).Contains(e.X, e.Y)
                        ? _buttonHover
                        : _buttonNormal;
                else
                    _drawSprite = _buttonNormal;

            return base.MouseUp(e);
        }

        public event ImageButtonPressHandler Clicked;
    }
}
