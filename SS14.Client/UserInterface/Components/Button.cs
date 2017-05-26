using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.Maths;
using System;

namespace SS14.Client.UserInterface.Components
{
    internal class Button : GuiComponent
    {
        #region Delegates

        public delegate void ButtonPressHandler(Button sender);

        #endregion

        private readonly IResourceManager _resourceManager;

        private Sprite _buttonLeft;
        private Sprite _buttonMain;
        private Sprite _buttonRight;

        private IntRect _clientAreaLeft;
        private IntRect _clientAreaMain;
        private IntRect _clientAreaRight;

        private Color drawColor = Color.White;
        public Color mouseOverColor = Color.White;

        public Button(string buttonText, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            _buttonLeft = _resourceManager.GetSprite("button_left");
            _buttonMain = _resourceManager.GetSprite("button_middle");
            _buttonRight = _resourceManager.GetSprite("button_right");

            Label = new TextSprite("ButtonLabel" + buttonText, buttonText, _resourceManager.GetFont("CALIBRI"))
                        {
                            Color = Color.Black
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
            _clientAreaLeft = new IntRect(Position, new Vector2i((int)boundsLeft.Width, (int)boundsLeft.Height));
            _clientAreaMain = new IntRect(_clientAreaLeft.Right(), Position.Y,
                                            (int) Label.Width, (int)boundsMain.Height);
            _clientAreaRight = new IntRect(_clientAreaMain.Right(), Position.Y,
                                             (int)boundsRight.Width, (int)boundsRight.Height);
            ClientArea = new IntRect(Position,
                                       new Vector2i(_clientAreaLeft.Width + _clientAreaMain.Width + _clientAreaRight.Width,
                                                Math.Max(Math.Max(_clientAreaLeft.Height, _clientAreaRight.Height), _clientAreaMain.Height)));
            Label.Position = new Vector2i(_clientAreaLeft.Right(),
                                       Position.Y + (int) (ClientArea.Height/2f) - (int) (Label.Height/2f));
        }

        public override void Render()
        {
            _buttonLeft.Color = drawColor;
            _buttonMain.Color = drawColor;
            _buttonRight.Color = drawColor;
            
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
            if (mouseOverColor != Color.White)
                if (ClientArea.Contains(e.X, e.Y))
                    drawColor = mouseOverColor;
                else
                    drawColor = Color.White;
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(e.X, e.Y))
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