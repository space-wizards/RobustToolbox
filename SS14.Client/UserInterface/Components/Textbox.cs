using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.ResourceManagement;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;

namespace SS14.Client.UserInterface.Components
{
    internal class Textbox : GuiComponent
    {
        #region Delegates

        public delegate void TextSubmitHandler(string text, Textbox sender);

        #endregion Delegates

        private readonly IResourceCache _resourceCache;
        public bool ClearFocusOnSubmit = true;
        public bool ClearOnSubmit = true;
        public bool AllowEmptySubmit = true;

        public TextSprite Label;
        public int MaxCharacters = 255;
        public int Width;
        private float _caretHeight = 12;
        private int _caretIndex;
        private float _caretPos;
        private float _caretWidth = 2;

        private IntRect _clientAreaLeft;
        private IntRect _clientAreaMain;
        private IntRect _clientAreaRight;

        private int _displayIndex;

        private string _displayText = "";
        private string _text = "";
        private Sprite _textboxLeft;
        private Sprite _textboxMain;
        private Sprite _textboxRight;

        public Color drawColor = Color.White;
        public Color textColor = Color.Black;

        private float blinkCount;

        // Terrible hack to get around TextEntered AND KeyDown firing at once.
        // Set to true after handling a KeyDown that produces a character to this.
        public bool ignoreNextText = false;

        public Textbox(int width, IResourceCache resourceCache)
        {
            _resourceCache = resourceCache;
            _textboxLeft = _resourceCache.GetSprite("text_left");
            _textboxMain = _resourceCache.GetSprite("text_middle");
            _textboxRight = _resourceCache.GetSprite("text_right");

            Width = width;

            Label = new TextSprite("Textbox", "", _resourceCache.GetResource<FontResource>(@"Fonts/CALIBRI.TTF").Font) { Color = Color.Black };

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
            var boundsLeft = _textboxLeft.GetLocalBounds();
            var boundsMain = _textboxMain.GetLocalBounds();
            var boundsRight = _textboxRight.GetLocalBounds();
            _clientAreaLeft = new IntRect(Position, new Vector2i((int)boundsLeft.Width, (int)boundsLeft.Height));

            _clientAreaMain = new IntRect(_clientAreaLeft.Right(), Position.Y,
                                            Width, (int)boundsMain.Height);
            _clientAreaRight = new IntRect(_clientAreaMain.Right(), Position.Y,
                                             (int)boundsRight.Width, (int)boundsRight.Height);
            ClientArea = new IntRect(Position,
                                       new Vector2i(_clientAreaLeft.Width + _clientAreaMain.Width + _clientAreaRight.Width,
                                                Math.Max(Math.Max(_clientAreaLeft.Height, _clientAreaRight.Height),
                                                         _clientAreaMain.Height)));
            Label.Position = new Vector2i(_clientAreaLeft.Right(),
                                       Position.Y + (int)(ClientArea.Height / 2f) - (int)(Label.Height / 2f));

            if (Focus)
            {
                blinkCount += 1 * frameTime;
                if (blinkCount > 0.50f) blinkCount = 0;
            }
        }

        public override void Render()
        {
            if (drawColor != Color.White)
            {
                _textboxLeft.Color = drawColor;
                _textboxMain.Color = drawColor;
                _textboxRight.Color = drawColor;
            }

            _textboxLeft.SetTransformToRect(_clientAreaLeft);
            _textboxMain.SetTransformToRect(_clientAreaMain);
            _textboxRight.SetTransformToRect(_clientAreaRight);
            _textboxLeft.Draw();
            _textboxMain.Draw();
            _textboxRight.Draw();

            if (Focus && blinkCount <= 0.25f)
                CluwneLib.drawRectangle(Label.Position.X + _caretPos - _caretWidth, Label.Position.Y + (Label.Height / 2f) - (_caretHeight / 2f), _caretWidth, _caretHeight, new Color(255, 255, 250));

            if (drawColor != Color.White)
            {
                _textboxLeft.Color = Color.White;
                _textboxMain.Color = Color.White;
                _textboxRight.Color = Color.White;
            }

            Label.Color = textColor;
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

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(e.X, e.Y))
            {
                return true;
            }

            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            return false;
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            if (!Focus) return false;

            if (e.Control && e.Code == Keyboard.Key.V)
            {
                string ret = System.Windows.Forms.Clipboard.GetText();
                Text = Text.Insert(_caretIndex, ret);
                if (_caretIndex < _text.Length) _caretIndex += ret.Length;
                SetVisibleText();
                ignoreNextText = true;
                return true;
            }

            if (e.Control && e.Code == Keyboard.Key.C)
            {
                System.Windows.Forms.Clipboard.SetText(Text);
                ignoreNextText = true;
                return true;
            }

            // Control + Backspace to delete all text
            if (e.Control && e.Code == Keyboard.Key.BackSpace && Text.Length >= 1)
            {
                Clear();
                ignoreNextText = true;
                return true;
            }

            if (e.Code == Keyboard.Key.Left)
            {
                if (_caretIndex > 0) _caretIndex--;
                SetVisibleText();
                return true;
            }
            else if (e.Code == Keyboard.Key.Right)
            {
                if (_caretIndex < _text.Length) _caretIndex++;
                SetVisibleText();
                return true;
            }

            if (e.Code == Keyboard.Key.Return && (AllowEmptySubmit || Text.Length >= 1))
            {
                Submit();
                return true;
            }

            if (e.Code == Keyboard.Key.BackSpace && Text.Length >= 1)
            {
                if (_caretIndex == 0) return true;

                Text = Text.Remove(_caretIndex - 1, 1);
                if (_caretIndex > 0 && _caretIndex < Text.Length) _caretIndex--;
                SetVisibleText();
                return true;
            }

            if (e.Code == Keyboard.Key.Delete && Text.Length >= 1)
            {
                if (_caretIndex >= Text.Length) return true;
                Text = Text.Remove(_caretIndex, 1);
                SetVisibleText();
                return true;
            }

            return true;
        }

        public override bool TextEntered(TextEventArgs e)
        {
            if (Text.Length >= MaxCharacters || "\b\n\u001b\r".Contains(e.Unicode))
                return false;

            if (ignoreNextText)
            {
                ignoreNextText = false;
                return false;
            }

            Text = Text.Insert(_caretIndex, e.Unicode);
            if (_caretIndex < _text.Length) _caretIndex++;
            SetVisibleText();
            return true;
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

                if (Text.Length <= _caretIndex - 1)
                    _caretIndex = Text.Length;
                _caretPos = 
                            Label.MeasureLine(Text.Substring(_displayIndex, _caretIndex - _displayIndex));
            }
        }

        private void Submit()
        {
            if (OnSubmit != null) OnSubmit(Text, this);
            if (ClearOnSubmit)
            {
                Clear();
            }
            if (ClearFocusOnSubmit)
            {
                Focus = false;
                IoCManager.Resolve<IUserInterfaceManager>().RemoveFocus(this);
            }
        }

        public void Clear()
        {
            Text = string.Empty;
        }
    }
}
