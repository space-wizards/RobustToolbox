using System;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Resource;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using System.Diagnostics;

namespace ClientServices.UserInterface.Components
{
    class Textbox : GuiComponent
    {
        private readonly IResourceManager _resourceManager;

        private Sprite _textboxMain;
        private Sprite _textboxLeft;
        private Sprite _textboxRight;

        public TextSprite Label;

        public delegate void TextSubmitHandler(string text, Textbox sender);
        public event TextSubmitHandler OnSubmit;

        private Rectangle _clientAreaMain;
        private Rectangle _clientAreaLeft;
        private Rectangle _clientAreaRight;

        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                SetVisibleText();
            }
        }

        private string text = "";
        private string displayText = "";

        public bool ClearOnSubmit = true;
        public bool ClearFocusOnSubmit = true;
        public int MaxCharacters = 20;
        public int Width;

        private byte blinkCount = 0; //Look at this shitty framerate dependant blinking bar. What a load of shit. I dont care!

        public Textbox(int width, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            _textboxLeft = _resourceManager.GetSprite("text_left");
            _textboxMain = _resourceManager.GetSprite("text_middle");
            _textboxRight = _resourceManager.GetSprite("text_right");

            Width = width;

            Label = new TextSprite("Textbox", "", _resourceManager.GetFont("CALIBRI")) {Color = Color.Black};

            Update(0);
        }

        public override void Update(float frameTime)
        {

            _clientAreaLeft = new Rectangle(Position, new Size((int)_textboxLeft.Width, (int)_textboxLeft.Height));
            _clientAreaMain = new Rectangle(new Point(_clientAreaLeft.Right, Position.Y), new Size(Width, (int)_textboxMain.Height));
            _clientAreaRight = new Rectangle(new Point(_clientAreaMain.Right, Position.Y), new Size((int)_textboxRight.Width, (int)_textboxRight.Height));
            ClientArea = new Rectangle(Position, new Size(_clientAreaLeft.Width + _clientAreaMain.Width + _clientAreaRight.Width, Math.Max(Math.Max(_clientAreaLeft.Height,_clientAreaRight.Height), _clientAreaMain.Height)));
            Label.Position = new Point(_clientAreaLeft.Right, Position.Y + (int)(ClientArea.Height / 2f) - (int)(Label.Height / 2f));

            if (Focus) Label.Text = displayText + (blinkCount++ < 100 ? "|" : "");
            else Label.Text = displayText;

            if (blinkCount > 150) blinkCount = 0;
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
                SetVisibleText();
                return true;
            }

            if (char.IsLetterOrDigit(e.CharacterMapping.Character) || char.IsPunctuation(e.CharacterMapping.Character))
            {
                if (Text.Length == MaxCharacters) return false;
                if (e.Shift)
                {
                    Text += e.CharacterMapping.Shifted;
                    SetVisibleText();
                }
                else
                {
                    Text += e.CharacterMapping.Character;
                    SetVisibleText();
                }
                return true;
            }
            return false;
        }

        private void SetVisibleText()
        {
            displayText = "";
            int index = -1;

            while (Label.MeasureLine(displayText + "|") < _clientAreaMain.Width && ++index <= Text.Length)
                displayText = Text.Substring(Text.Length - index, index);
        }

        private void Submit()
        {
            if (OnSubmit != null) OnSubmit(Text, this);
            if (ClearOnSubmit)
            {
                Text = string.Empty;
                displayText = string.Empty;
            }
            if (ClearFocusOnSubmit) Focus = false;
        }
    }
}
