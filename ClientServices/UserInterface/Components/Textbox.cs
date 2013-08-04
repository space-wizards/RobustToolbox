using System;
using System.Drawing;
using ClientInterfaces.Resource;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientServices.UserInterface.Components
{
    internal class Textbox : GuiComponent
    {
        #region Delegates

        public delegate void TextSubmitHandler(string text, Textbox sender);

        #endregion

        private readonly IResourceManager _resourceManager;
        public bool ClearFocusOnSubmit = true;
        public bool ClearOnSubmit = true;

        public TextSprite Label;
        public int MaxCharacters = 255;
        public int Width;
        private float _caretHeight = 12;
        private int _caretIndex;
        private float _caretPos;
        private float _caretWidth = 2;

        private Rectangle _clientAreaLeft;
        private Rectangle _clientAreaMain;
        private Rectangle _clientAreaRight;

        private int _displayIndex;

        private string _displayText = "";
        private string _text = "";
        private Sprite _textboxLeft;
        private Sprite _textboxMain;
        private Sprite _textboxRight;

        private float blinkCount;

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

        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                SetVisibleText();
            }
        }

        public event TextSubmitHandler OnSubmit;

        public override void Update(float frameTime)
        {
            _clientAreaLeft = new Rectangle(Position, new Size((int) _textboxLeft.Width, (int) _textboxLeft.Height));
            _clientAreaMain = new Rectangle(new Point(_clientAreaLeft.Right, Position.Y),
                                            new Size(Width, (int) _textboxMain.Height));
            _clientAreaRight = new Rectangle(new Point(_clientAreaMain.Right, Position.Y),
                                             new Size((int) _textboxRight.Width, (int) _textboxRight.Height));
            ClientArea = new Rectangle(Position,
                                       new Size(_clientAreaLeft.Width + _clientAreaMain.Width + _clientAreaRight.Width,
                                                Math.Max(Math.Max(_clientAreaLeft.Height, _clientAreaRight.Height),
                                                         _clientAreaMain.Height)));
            Label.Position = new Point(_clientAreaLeft.Right,
                                       Position.Y + (int) (ClientArea.Height/2f) - (int) (Label.Height/2f));

            if (Focus)
            {
                blinkCount += 1*frameTime;
                if (blinkCount > 0.50f) blinkCount = 0;
            }
        }

        public override void Render()
        {
            _textboxLeft.Draw(_clientAreaLeft);
            _textboxMain.Draw(_clientAreaMain);
            _textboxRight.Draw(_clientAreaRight);

            if (Focus && blinkCount <= 0.25f)
                Gorgon.CurrentRenderTarget.FilledRectangle(_caretPos - _caretWidth,
                                                           Label.Position.Y + (Label.Height/2f) - (_caretHeight/2f),
                                                           _caretWidth, _caretHeight, Color.WhiteSmoke);

            Label.Text = _displayText;
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
                if (_caretIndex == 0) return true;

                Text = Text.Remove(_caretIndex - 1, 1);
                if (_caretIndex > 0) _caretIndex--;
                SetVisibleText();
                return true;
            }

            if (char.IsLetterOrDigit(e.CharacterMapping.Character) || char.IsPunctuation(e.CharacterMapping.Character) ||
                char.IsWhiteSpace(e.CharacterMapping.Character))
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
                if (_caretIndex < _displayIndex)
                    //Caret outside to the left. Move display text to the left by setting its index to the caret.
                    _displayIndex = _caretIndex;

                int glyphCount = 0;

                while (_displayIndex + (glyphCount + 1) < _text.Length &&
                       Label.MeasureLine(Text.Substring(_displayIndex + 1, glyphCount + 1)) < _clientAreaMain.Width)
                    glyphCount++; //How many glyphs we could/would draw with the current index.

                if (_caretIndex > _displayIndex + glyphCount) //Caret outside?
                {
                    if (_text.Substring(_displayIndex + 1).Length != glyphCount) //Still stuff outside the screen?
                    {
                        _displayIndex++;
                        //Increase display index by one since the carret is one outside to the right. But only if there's still letters to the right.

                        glyphCount = 0; //Update glyphcount with new index.

                        while (_displayIndex + (glyphCount + 1) < _text.Length &&
                               Label.MeasureLine(Text.Substring(_displayIndex + 1, glyphCount + 1)) <
                               _clientAreaMain.Width)
                            glyphCount++;
                    }
                }
                _displayText = Text.Substring(_displayIndex + 1, glyphCount);

                _caretPos = Label.Position.X +
                            Label.MeasureLine(Text.Substring(_displayIndex, _caretIndex - _displayIndex));
            }
            else //Text fits completely inside box.
            {
                _displayIndex = 0;
                _displayText = Text;

                if (Text.Length < _caretIndex - 1)
                    _caretIndex = Text.Length;
                _caretPos = Label.Position.X +
                            Label.MeasureLine(Text.Substring(_displayIndex, _caretIndex - _displayIndex));
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