using System;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Resource;
using GorgonLibrary;
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

        private int _caretIndex = 0;
        private int _displayIndex = 0;

        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                SetVisibleText();
            }
        }

        private string _text = "";
        private string _displayText = "";

        public bool ClearOnSubmit = true;
        public bool ClearFocusOnSubmit = true;
        public int MaxCharacters = 255;
        public int Width;

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

            if (!Focus)
                _caretIndex = _text.Length;
        }

        public override void Render()
        {
            _textboxLeft.Draw(_clientAreaLeft);
            _textboxMain.Draw(_clientAreaMain);
            _textboxRight.Draw(_clientAreaRight);

            Label.Text = _displayText;
            Label.Draw();

            const float barHeightAdj = 5f; //Amount of pixels to SHORTEN the bars height by. Negative means LONGER. Temporary.

            Vector2D barPos;

            string str = Text.Substring(_displayIndex, _caretIndex - _displayIndex); //When scrolling backwards , could display index be higher than caretindex?
            float carretx = Label.MeasureLine(str);

            barPos.X = Label.Position.X + carretx;
            barPos.Y = Label.Position.Y + barHeightAdj;

            Gorgon.CurrentRenderTarget.FilledRectangle(barPos.X, barPos.Y, 1, Label.Height - (barHeightAdj * 2), Color.HotPink);
            Gorgon.CurrentRenderTarget.Rectangle(Label.Position.X, Label.Position.Y, Label.Width, Label.Height, Color.DarkRed);
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
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
            {
                return true; 
            }

            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            if (!Focus) return false;

            if (e.Key == KeyboardKeys.Left)
            {
                if (_caretIndex > 0) _caretIndex--;
                SetVisibleText();
                return true;
            }
            else if (e.Key == KeyboardKeys.Right)
            {
                if (_caretIndex < _text.Length) _caretIndex++;
                SetVisibleText();
                return true;
            }

            if (e.Key == KeyboardKeys.Return && Text.Length >= 1)
            {
                Submit();
                return true;
            }

            if (e.Key == KeyboardKeys.Back && Text.Length >= 1)
            {
                Text = Text.Remove(_caretIndex - 1, 1);
                if (_caretIndex > 0) _caretIndex--;
                SetVisibleText();
                return true;
            }

            if (char.IsLetterOrDigit(e.CharacterMapping.Character) || char.IsPunctuation(e.CharacterMapping.Character) || char.IsWhiteSpace(e.CharacterMapping.Character))
            {
                if (Text.Length == MaxCharacters) return false;
                if (e.Shift)
                {
                    Text = Text.Insert(_caretIndex, e.CharacterMapping.Shifted.ToString());
                    if (_caretIndex < _text.Length) _caretIndex++;
                    SetVisibleText();
                }
                else
                {
                    Text = Text.Insert(_caretIndex, e.CharacterMapping.Character.ToString());
                    if (_caretIndex < _text.Length) _caretIndex++;
                    SetVisibleText();
                }
                return true;
            }
            return false;
        }

        private void SetVisibleText() 
        {
            _displayText = "";

            if (Label.MeasureLine(_text) >= _clientAreaMain.Width) //Text wider than box.
            {
                if (_caretIndex < _displayIndex) //Caret outside to the left. Move display text to the left by setting its index to the caret.
                    _displayIndex = _caretIndex;

                int glyphCount = 0;

                while (_displayIndex + (glyphCount + 1) < _text.Length && Label.MeasureLine(Text.Substring(_displayIndex, glyphCount + 1)) < _clientAreaMain.Width)
                    glyphCount++;
                //Now we have the number of letters we can display with the current index.

                //if (_text.Substring(_displayIndex).Length == glyphCount)

                //Since we now know how many glyphs we can draw, we can say whether the caret is outside to the right.
                if (_caretIndex > _displayIndex + glyphCount) 
                {
                    _displayIndex++; //Increase display index by one since the carret is one outside to the right.
                    glyphCount = 0;  //Reset to 0 since we need to check the length again with the new index.

                    while (_displayIndex + (glyphCount + 1) < _text.Length && Label.MeasureLine(Text.Substring(_displayIndex, glyphCount + 1)) < _clientAreaMain.Width)
                        glyphCount++;
                }

                _displayText = Text.Substring(_displayIndex, glyphCount);
            }
            else //Text fits completely inside box.
            {
                _displayIndex = 0;
                _displayText = Text;
            }
        }

        private void Submit()
        {
            if (OnSubmit != null) OnSubmit(Text, this);
            if (ClearOnSubmit)
            {
                Text = string.Empty;
                _displayText = string.Empty;
            }
            if (ClearFocusOnSubmit) Focus = false;
        }
    }
}
