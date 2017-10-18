using OpenTK.Graphics;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Graphics.Utility;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.Maths;
using System;
using SS14.Client.ResourceManagement;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.Components
{
    internal class Button : GuiComponent
    {
        #region Delegates

        public delegate void ButtonPressHandler(Button sender);

        #endregion

        private readonly IResourceCache _resourceCache;

        private Sprite _buttonLeft;
        private Sprite _buttonMain;
        private Sprite _buttonRight;

        private Box2i _clientAreaLeft;
        private Box2i _clientAreaMain;
        private Box2i _clientAreaRight;

        private Color4 drawColor = Color4.White;
        public Color4 mouseOverColor = Color4.White;

        public Button(string buttonText, IResourceCache resourceCache)
        {
            _resourceCache = resourceCache;
            _buttonLeft = _resourceCache.GetSprite("button_left");
            _buttonMain = _resourceCache.GetSprite("button_middle");
            _buttonRight = _resourceCache.GetSprite("button_right");

            Label = new TextSprite("ButtonLabel" + buttonText, buttonText, _resourceCache.GetResource<FontResource>("Fonts/CALIBRI.TTF").Font)
                        {
                            Color = Color4.Black
                        };

            Update(0);
        }

        public TextSprite Label { get; private set; }

        public event ButtonPressHandler Clicked;

        public override sealed void Update(float frameTime)
        {
            var boundsLeft = _buttonLeft.GetLocalBounds();
            var boundsMain = _buttonMain.GetLocalBounds();
            var boundsRight = _buttonRight.GetLocalBounds();
            _clientAreaLeft = Box2i.FromDimensions(Position, new Vector2i((int)boundsLeft.Width, (int)boundsLeft.Height));
            _clientAreaMain = Box2i.FromDimensions(_clientAreaLeft.Right, Position.Y,
                                            (int) Label.Width, (int)boundsMain.Height);
            _clientAreaRight = Box2i.FromDimensions(_clientAreaMain.Right, Position.Y,
                                             (int)boundsRight.Width, (int)boundsRight.Height);
            ClientArea = Box2i.FromDimensions(Position,
                                       new Vector2i(_clientAreaLeft.Width + _clientAreaMain.Width + _clientAreaRight.Width,
                                                Math.Max(Math.Max(_clientAreaLeft.Height, _clientAreaRight.Height), _clientAreaMain.Height)));
            Label.Position = new Vector2i(_clientAreaLeft.Right,
                                       Position.Y + (int) (ClientArea.Height/2f) - (int) (Label.Height/2f));
        }

        public override void Render()
        {
            _buttonLeft.Color = drawColor.Convert();
            _buttonMain.Color = drawColor.Convert();
            _buttonRight.Color = drawColor.Convert();

            _buttonLeft.SetTransformToRect(_clientAreaLeft);
            _buttonMain.SetTransformToRect(_clientAreaMain);
            _buttonRight.SetTransformToRect(_clientAreaRight);
            _buttonLeft.Draw();
            _buttonMain.Draw();
            _buttonRight.Draw();

            _buttonLeft.Color = Color.White;
            _buttonMain.Color = Color.White;
            _buttonRight.Color = Color.White;

            Label.Draw();
        }

        public override void Dispose()
        {
            Label = null;
            _buttonLeft = null;
            _buttonMain = null;
            _buttonRight = null;
            Clicked = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            if (mouseOverColor != Color4.White)
                if (ClientArea.Contains(new Vector2i(e.X, e.Y)))
                    drawColor = mouseOverColor;
                else
                    drawColor = Color4.White;
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Vector2i(e.X, e.Y)))
            {
                if (Clicked != null) Clicked(this);
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            return false;
        }
    }
}
