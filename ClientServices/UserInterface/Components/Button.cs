using System;
using System.Drawing;
using ClientInterfaces;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientServices.UserInterface.Components
{
    class Button : GuiComponent
    {
        private readonly IResourceManager _resourceManager;

        private Sprite _buttonMain;
        private Sprite _buttonLeft;
        private Sprite _buttonRight;

        private Rectangle _clientAreaMain;
        private Rectangle _clientAreaLeft;
        private Rectangle _clientAreaRight;

        public TextSprite Label { get; private set; }

        public delegate void ButtonPressHandler(Button sender);
        public event ButtonPressHandler Clicked;



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

            Update();
        }

        public override sealed void Update()
        {
            _clientAreaLeft = new Rectangle(Position, new Size((int)_buttonLeft.Width, (int)_buttonLeft.Height));
            _clientAreaMain = new Rectangle(new Point(_clientAreaLeft.Right, Position.Y), new Size((int)Label.Width, (int)_buttonMain.Height));
            _clientAreaRight = new Rectangle(new Point(_clientAreaMain.Right, Position.Y), new Size((int)_buttonRight.Width, (int)_buttonRight.Height));
            ClientArea = new Rectangle(Position, new Size(_clientAreaLeft.Width + _clientAreaMain.Width + _clientAreaRight.Width, _clientAreaMain.Height));
            Label.Position = new Point(_clientAreaLeft.Right, Position.Y + (int)(ClientArea.Height / 2f) - (int)(Label.Height / 2f));
        }

        public override void Render()
        {
            _buttonLeft.Draw(_clientAreaLeft);
            _buttonMain.Draw(_clientAreaMain);
            _buttonRight.Draw(_clientAreaRight);
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

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                if (Clicked != null) Clicked(this);
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }
    }
}
