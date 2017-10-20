using OpenTK.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Graphics.Utility;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.Components
{
    public class ImageButton : GuiComponent
    {
        #region Delegates

        public delegate void ImageButtonPressHandler(ImageButton sender);

        #endregion Delegates

        private readonly IResourceCache _resourceCache;
        private Sprite _buttonClick;
        private Sprite _buttonHover;
        private Sprite _buttonNormal;

        private Sprite _drawSprite;

        public ImageButton()
        {
            _resourceCache = IoCManager.Resolve<IResourceCache>();
            Color = Color4.White;
            Update(0);
        }

        public Color4 Color { get; set; }

        public string ImageNormal
        {
            set { _buttonNormal = _resourceCache.GetSprite(value); }
        }

        public string ImageHover
        {
            set { _buttonHover = _resourceCache.GetSprite(value); }
        }

        public string ImageClick
        {
            set { _buttonClick = _resourceCache.GetSprite(value); }
        }

        public event ImageButtonPressHandler Clicked;

        public override sealed void Update(float frameTime)
        {
            if (_drawSprite == null && _buttonNormal != null)
                _drawSprite = _buttonNormal;

            if (_drawSprite != null)
            {
                _drawSprite.Position = Position;
                var bounds = _drawSprite.LocalBounds;
                ClientArea = Box2i.FromDimensions(Position,
                                           new Vector2i((int)bounds.Width, (int)bounds.Height));
            }
        }

        public override void Render()
        {
            if (_drawSprite != null)
            {
                _drawSprite.Color = Color;
                _drawSprite.Position = Position;
                _drawSprite.Texture.Smooth = true;
                _drawSprite.Draw(Graphics.CluwneLib.CurrentRenderTarget, new RenderStates(BlendMode.Alpha));
                _drawSprite.Color = Color;
            }
        }

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
            if (ClientArea.Contains(e.X, e.Y) && _buttonHover != null)
            {
                if (_drawSprite != _buttonClick)
                    _drawSprite = _buttonHover;
            }
            else
            {
                if (_drawSprite != _buttonClick)
                    _drawSprite = _buttonNormal;
            }
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(e.X, e.Y))
            {
                if (_buttonClick != null) _drawSprite = _buttonClick;
                if (Clicked != null) Clicked(this);
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (_drawSprite == _buttonClick)
                if (_buttonHover != null)
                    _drawSprite = ClientArea.Contains(e.X, e.Y)
                                      ? _buttonHover
                                      : _buttonNormal;
                else
                    _drawSprite = _buttonNormal;
            return false;
        }
    }
}
