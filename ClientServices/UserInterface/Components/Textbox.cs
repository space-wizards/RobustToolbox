using System;
using System.Drawing;
using ClientInterfaces;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientServices.UserInterface.Components
{
    class Textbox : GuiComponent
    {
        private readonly IResourceManager _resourceManager;

        private Sprite _textboxMain;
        private Sprite _textboxLeft;
        private Sprite _textboxRight;

        public TextSprite Label;

        public delegate void TextSubmitHandler(string text);
        public event TextSubmitHandler OnSubmit;

        private Rectangle _clientAreaMain;
        private Rectangle _clientAreaLeft;
        private Rectangle _clientAreaRight;

        public string Text;
        public bool ClearOnSubmit = true;
        public bool ClearFocusOnSubmit = true;
        public int MaxCharacters = 20;
        public int Width;

        public Textbox(int width, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            _textboxLeft = _resourceManager.GetSprite("button_left");
            _textboxMain = _resourceManager.GetSprite("button_middle");
            _textboxRight = _resourceManager.GetSprite("button_right");

            Width = width;

            Label = new TextSprite("Textbox", "", _resourceManager.GetFont("CALIBRI")) {Color = Color.Black};

            Update();
        }

        public override void Update()
        {

            _clientAreaLeft = new Rectangle(Position, new Size((int)_textboxLeft.Width, (int)_textboxLeft.Height));
            _clientAreaMain = new Rectangle(new Point(_clientAreaLeft.Right, Position.Y), new Size(Width, (int)_textboxMain.Height));
            _clientAreaRight = new Rectangle(new Point(_clientAreaMain.Right, Position.Y), new Size((int)_textboxRight.Width, (int)_textboxRight.Height));
            ClientArea = new Rectangle(Position, new Size(_clientAreaLeft.Width + _clientAreaMain.Width + _clientAreaRight.Width, _clientAreaMain.Height));
            Label.Position = new Point(_clientAreaLeft.Right, Position.Y + (int)(ClientArea.Height / 2f) - (int)(Label.Height / 2f));

            if (Focus) Label.Text = Text + "|";
            else Label.Text = Text;
        }

        public override void Render()
        {
            _textboxLeft.Draw(_clientAreaLeft);
            _textboxMain.Draw(_clientAreaMain);
            _textboxRight.Draw(_clientAreaRight);
            Label.Draw();
        }

        public override void Dispose()
        {
            Label = null;
            _textboxLeft = null;
            _textboxMain = null;
            _textboxRight = null;
            OnSubmit = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y))) //Needed so it grabs focus when clicked.
                return true;

            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            if (!Focus) return false;

            if (e.Key == KeyboardKeys.Return && Text.Length >= 1)
            {
                Submit();
                return true;
            }

            if (e.Key == KeyboardKeys.Back && Text.Length >= 1)
            {
                Text = Text.Substring(0, Text.Length - 1);
                return true;
            }

            if (char.IsLetterOrDigit(e.CharacterMapping.Character))
            {
                if (Text.Length == MaxCharacters) return false;
                if (e.Shift)
                {
                    Text += e.CharacterMapping.Shifted;
                }
                else
                {
                    Text += e.CharacterMapping.Character;
                }
                return true;
            }
            return false;
        }

        private void Submit()
        {
            if (OnSubmit != null) OnSubmit(Text);
            if (ClearOnSubmit) Text = string.Empty;
            if (ClearFocusOnSubmit) Focus = false;
        }
    }
}
