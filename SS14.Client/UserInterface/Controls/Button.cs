using System;
using OpenTK.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Graphics.Sprites;
using SS14.Client.ResourceManagement;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    internal class Button : Control
    {
        public delegate void ButtonPressHandler(Button sender);

        public Color4 MouseOverColor = Color4.White;

        private Sprite _buttonLeft;
        private Sprite _buttonMain;
        private Sprite _buttonRight;

        private Box2i _clientAreaLeft;
        private Box2i _clientAreaMain;
        private Box2i _clientAreaRight;

        private Color4 _drawColor = Color4.White;

        public TextSprite Label { get; private set; }

        public Button(string buttonText)
        {
            _buttonLeft = _resourceCache.GetSprite("button_left");
            _buttonMain = _resourceCache.GetSprite("button_middle");
            _buttonRight = _resourceCache.GetSprite("button_right");

            Label = new TextSprite(buttonText, _resourceCache.GetResource<FontResource>("Fonts/CALIBRI.TTF").Font)
            {
                FillColor = Color4.Black
            };
        }

        /// <inheritdoc />
        protected override void OnCalcRect()
        {
            var boundsLeft = _buttonLeft.LocalBounds;
            var boundsMain = _buttonMain.LocalBounds;
            var boundsRight = _buttonRight.LocalBounds;

            _clientAreaLeft = Box2i.FromDimensions(new Vector2i(), new Vector2i((int) boundsLeft.Width, (int) boundsLeft.Height));
            _clientAreaMain = Box2i.FromDimensions(_clientAreaLeft.Right, 0, Label.Width, (int) boundsMain.Height);
            _clientAreaRight = Box2i.FromDimensions(_clientAreaMain.Right, 0, (int) boundsRight.Width, (int) boundsRight.Height);

            _clientArea = Box2i.FromDimensions(new Vector2i(),
                new Vector2i(_clientAreaLeft.Width + _clientAreaMain.Width + _clientAreaRight.Width,
                    Math.Max(Math.Max(_clientAreaLeft.Height, _clientAreaRight.Height), _clientAreaMain.Height)));
        }

        /// <inheritdoc />
        protected override void OnCalcPosition()
        {
            base.OnCalcPosition();

            Label.Position = Position + new Vector2i(_clientAreaLeft.Right, (int) (_clientArea.Height / 2f) - (int) (Label.Height / 2f));
        }

        /// <inheritdoc />
        protected override void DrawContents()
        {
            _buttonLeft.Color = _drawColor;
            _buttonMain.Color = _drawColor;
            _buttonRight.Color = _drawColor;

            _buttonLeft.SetTransformToRect(_clientAreaLeft.Translated(Position));
            _buttonMain.SetTransformToRect(_clientAreaMain.Translated(Position));
            _buttonRight.SetTransformToRect(_clientAreaRight.Translated(Position));
            _buttonLeft.Draw();
            _buttonMain.Draw();
            _buttonRight.Draw();

            _buttonLeft.Color = Color.White;
            _buttonMain.Color = Color.White;
            _buttonRight.Color = Color.White;

            Label.Draw();
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override void MouseMove(MouseMoveEventArgs e)
        {
            base.MouseMove(e);

            if (MouseOverColor == Color4.White)
                return;

            _drawColor = ClientArea.Translated(Position).Contains(new Vector2i(e.X, e.Y)) ? MouseOverColor : Color4.White;
        }

        /// <inheritdoc />
        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (base.MouseDown(e))
                return true;

            if (ClientArea.Translated(Position).Contains(new Vector2i(e.X, e.Y)))
            {
                Clicked?.Invoke(this);
                return true;
            }
            return false;
        }

        public event ButtonPressHandler Clicked;
    }
}
