using OpenTK.Graphics;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics.Utility;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;

namespace SS14.Client.UserInterface.Components
{
    public class ImageButton : GuiComponent
    {
        public delegate void ImageButtonPressHandler(ImageButton sender);
        
        private readonly IResourceCache _resourceCache;
        private Sprite _buttonClick;
        private Sprite _buttonHover;
        private Sprite _buttonNormal;
        private Sprite _drawSprite;

        public ImageButton()
        {
            _resourceCache = IoCManager.Resolve<IResourceCache>();
            Color = Color4.White;
        }

        public Color4 Color { get; set; }

        public string ImageNormal
        {
            set
            {
                _buttonNormal = _resourceCache.GetSprite(value);

                if (_drawSprite == null)
                    _drawSprite = _buttonNormal;

                OnCalcPosition();
            }
        }

        public string ImageHover
        {
            set => _buttonHover = _resourceCache.GetSprite(value);
        }

        public string ImageClick
        {
            set => _buttonClick = _resourceCache.GetSprite(value);
        }

        public event ImageButtonPressHandler Clicked;

        /// <inheritdoc />
        protected override void OnCalcRect()
        {
            if (_buttonNormal != null)
            {
                var bounds = _drawSprite.GetLocalBounds();
                _clientArea = new Box2i(0, 0, (int)bounds.Width, (int)bounds.Height);
            }

        }
        
        /// <inheritdoc />
        public override void Render()
        {
            base.Render();

            if (_drawSprite == null)
                return;

            _drawSprite.Color = Color.Convert();
            _drawSprite.Position = new Vector2f(Position.X, Position.Y); // mouse events swap _drawSprite at any time, need to be kept in sync here
            _drawSprite.Texture.Smooth = true;
            _drawSprite.Draw(Graphics.CluwneLib.CurrentRenderTarget, new RenderStates(BlendMode.Alpha));
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            _buttonNormal = null;
            _buttonHover = null;
            _buttonClick = null;
            Clicked = null;
            base.Dispose();
            GC.SuppressFinalize(this);
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
    }
}
