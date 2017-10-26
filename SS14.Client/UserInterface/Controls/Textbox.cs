using System;
using System.Windows.Forms;
using OpenTK.Graphics;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.ResourceManagement;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using KeyEventArgs = SS14.Client.Graphics.Input.KeyEventArgs;

namespace SS14.Client.UserInterface.Controls
{
    internal class Textbox : Control
    {
        public delegate void TextSubmitHandler(string text, Textbox sender);

        private const float CaretHeight = 12;
        private const float CaretWidth = 2;

        public bool ClearFocusOnSubmit = true;
        public bool ClearOnSubmit = true;

        // Terrible hack to get around TextEntered AND KeyDown firing at once.
        // Set to true after handling a KeyDown that produces a character to this.
        public bool ignoreNextText;

        public int MaxCharacters = 255;
        private int _caretIndex;
        private float _caretPos;

        private Box2i _clientAreaLeft;
        private Box2i _clientAreaMain;
        private Box2i _clientAreaRight;

        private int _displayIndex;

        private string _displayText = "";
        private string _text = "";
        private Sprite _textboxLeft;
        private Sprite _textboxMain;
        private Sprite _textboxRight;

        private float blinkCount;

        private TextSprite _textSprite;
        public bool AllowEmptySubmit { get; set; } = true;

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                SetVisibleText();
            }
        }

        public Textbox(int width)
        {
            _textboxLeft = new Sprite(_resourceCache.GetSprite("text_left"));
            _textboxMain = new Sprite(_resourceCache.GetSprite("text_middle"));
            _textboxRight = new Sprite(_resourceCache.GetSprite("text_right"));

            _textSprite = new TextSprite("", _resourceCache.GetResource<FontResource>(@"Fonts/CALIBRI.TTF"));

            Width = width;
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (Focus)
            {
                blinkCount += 1 * frameTime;
                if (blinkCount > 0.50f) blinkCount = 0;
            }
        }

        /// <inheritdoc />
        protected override void OnCalcRect()
        {
            var boundsLeft = _textboxLeft.LocalBounds;
            var boundsMain = _textboxMain.LocalBounds;
            var boundsRight = _textboxRight.LocalBounds;

            _clientAreaLeft = Box2i.FromDimensions(new Vector2i(), new Vector2i((int) boundsLeft.Width, (int) boundsLeft.Height));
            _clientAreaMain = Box2i.FromDimensions(_clientAreaLeft.Right, 0, Width, (int) boundsMain.Height);
            _clientAreaRight = Box2i.FromDimensions(_clientAreaMain.Right, 0, (int) boundsRight.Width, (int) boundsRight.Height);

            _clientArea = Box2i.FromDimensions(new Vector2i(),
                new Vector2i(_clientAreaLeft.Width + _clientAreaMain.Width + _clientAreaRight.Width,
                    Math.Max(Math.Max(_clientAreaLeft.Height, _clientAreaRight.Height),
                        _clientAreaMain.Height)));
        }

        /// <inheritdoc />
        protected override void OnCalcPosition()
        {
            base.OnCalcPosition();

            SetVisibleText();
            _textSprite.Position = Position + new Vector2i(_clientAreaMain.Left, (int) (_clientArea.Height / 2f) - (int) (_textSprite.Height / 2f));
        }

        /// <inheritdoc />
        public override void Draw()
        {
            if (BackgroundColor != Color4.White)
            {
                _textboxLeft.Color = BackgroundColor;
                _textboxMain.Color = BackgroundColor;
                _textboxRight.Color = BackgroundColor;
            }

            _textboxLeft.SetTransformToRect(_clientAreaLeft.Translated(Position));
            _textboxMain.SetTransformToRect(_clientAreaMain.Translated(Position));
            _textboxRight.SetTransformToRect(_clientAreaRight.Translated(Position));
            _textboxLeft.Draw();
            _textboxMain.Draw();
            _textboxRight.Draw();

            if (Focus && blinkCount <= 0.25f)
                CluwneLib.drawRectangle(_textSprite.Position.X + _caretPos - CaretWidth, _textSprite.Position.Y + _textSprite.Height / 2f - CaretHeight / 2f, CaretWidth, CaretHeight, new Color4(255, 255, 250, 255));

            _textSprite.FillColor = ForegroundColor;
            _textSprite.Text = _displayText;

            _textSprite.Draw();

            base.Draw();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            _textSprite = null;
            _textboxLeft = null;
            _textboxMain = null;
            _textboxRight = null;
            OnSubmit = null;

            base.Dispose();
        }

        /// <inheritdoc />
        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (base.MouseDown(e))
                return true;

            if (ClientArea.Translated(Position).Contains(e.X, e.Y))
            {
                Focus = true;
                return true;
            }

            return false;
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            if (base.KeyDown(e))
                return true;

            if (!Focus) return false;

            if (e.Control && e.Key == Keyboard.Key.V)
            {
                var ret = Clipboard.GetText();
                Text = Text.Insert(_caretIndex, ret);
                if (_caretIndex < _text.Length) _caretIndex += ret.Length;
                SetVisibleText();
                ignoreNextText = true;
                return true;
            }

            if (e.Control && e.Key == Keyboard.Key.C)
            {
                Clipboard.SetText(Text);
                ignoreNextText = true;
                return true;
            }

            // Control + Backspace to delete all text
            if (e.Control && e.Key == Keyboard.Key.BackSpace && Text.Length >= 1)
            {
                Clear();
                ignoreNextText = true;
                return true;
            }

            if (e.Key == Keyboard.Key.Left)
            {
                if (_caretIndex > 0) _caretIndex--;
                SetVisibleText();
                return true;
            }
            if (e.Key == Keyboard.Key.Right)
            {
                if (_caretIndex < _text.Length) _caretIndex++;
                SetVisibleText();
                return true;
            }

            if (e.Key == Keyboard.Key.Return && (AllowEmptySubmit || Text.Length >= 1))
            {
                Submit();
                return true;
            }

            if (e.Key == Keyboard.Key.BackSpace && Text.Length >= 1)
            {
                if (_caretIndex == 0) return true;

                Text = Text.Remove(_caretIndex - 1, 1);
                if (_caretIndex > 0 && _caretIndex < Text.Length) _caretIndex--;
                SetVisibleText();
                return true;
            }

            if (e.Key == Keyboard.Key.Delete && Text.Length >= 1)
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
            if (base.TextEntered(e))
                return true;

            if (!Focus) return false;

            if (Text.Length >= MaxCharacters || "\b\n\u001b\r".Contains(e.Text))
                return false;

            if (ignoreNextText)
            {
                ignoreNextText = false;
                return false;
            }

            Text = Text.Insert(_caretIndex, e.Text);
            if (_caretIndex < _text.Length) _caretIndex++;
            SetVisibleText();
            return true;
        }

        public int MeasureLine(string text)
        {
            return _textSprite.MeasureLine(text);
        }

        public event TextSubmitHandler OnSubmit;

        private void SetVisibleText()
        {
            _displayText = "";

            if (_textSprite.MeasureLine(_text) >= _clientAreaMain.Width) //Text wider than box.
            {
                if (_caretIndex < _displayIndex)
                    //Caret outside to the left. Move display text to the left by setting its index to the caret.
                    _displayIndex = _caretIndex;

                var glyphCount = 0;

                while (_displayIndex + glyphCount + 1 < _text.Length &&
                       _textSprite.MeasureLine(Text.Substring(_displayIndex + 1, glyphCount + 1)) < _clientAreaMain.Width)
                {
                    glyphCount++; //How many glyphs we could/would draw with the current index.
                }

                if (_caretIndex > _displayIndex + glyphCount) //Caret outside?
                    if (_text.Substring(_displayIndex + 1).Length != glyphCount) //Still stuff outside the screen?
                    {
                        _displayIndex++;
                        //Increase display index by one since the carret is one outside to the right. But only if there's still letters to the right.

                        glyphCount = 0; //Update glyphcount with new index.

                        while (_displayIndex + glyphCount + 1 < _text.Length &&
                               _textSprite.MeasureLine(Text.Substring(_displayIndex + 1, glyphCount + 1)) <
                               _clientAreaMain.Width)
                        {
                            glyphCount++;
                        }
                    }
                _displayText = Text.Substring(_displayIndex + 1, glyphCount);

                _caretPos = _textSprite.Position.X + _textSprite.MeasureLine(Text.Substring(_displayIndex, _caretIndex - _displayIndex));
            }
            else //Text fits completely inside box.
            {
                _displayIndex = 0;
                _displayText = Text;

                if (Text.Length <= _caretIndex - 1)
                    _caretIndex = Text.Length;

                _caretPos = _textSprite.MeasureLine(Text.Substring(_displayIndex, _caretIndex - _displayIndex));
            }
        }

        private void Submit()
        {
            OnSubmit?.Invoke(Text, this);

            if (ClearOnSubmit)
                Clear();

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
