using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics.Interface;

namespace SS14.Client.Graphics.Sprite
{
    /// <summary>
    /// Sprite that contains Text
    /// </summary>
    public class TextSprite : ICluwneDrawable
    {
        private bool _shadowed;                                    // Is the Text Shadowed
        private Color _shadowColor;                                // Shadow Color
        private Text _textSprite;
        private string Label;

        #region Constructors

        /// <summary>
        /// Creates a TextSprite
        /// </summary>
        /// <param name="Label"> Label of TextSprite </param>
        /// <param name="text"> Text to display </param>
        /// <param name="font"> Font to use when displaying Text </param>
        /// <param name="font"> Size of the font to use </param>
        public TextSprite(string Label, string text, Font font, uint size)
        {
            this.Label = Label;
            _textSprite = new Text(text, font, size);
        }

        /// <summary>
        /// Creates a TextSprite
        /// </summary>
        /// <param name="Label"> Label of TextSprite </param>
        /// <param name="text"> Text to display </param>
        /// <param name="font"> Font to use when displaying Text </param>
        public TextSprite(string Label, string text, Font font)
        {
            this.Label = Label;
            _textSprite = new Text(text, font, 14);
        }

        /// <summary>
        /// Creates a TextSprite
        /// </summary>
        /// <param name="Label"> ID of the TextSprite</param>
        /// <param name="x"> position X of TextSprite </param>
        /// <param name="y"> Position Y of TextSprite </param>
        /// <param name="width"> Width of TextSprite </param>
        /// <param name="height"> Height of TextSprite </param>
        //        public TextSprite(string Label, int x, int y, int width, int height)
        //        {
        //            this.Position = new Vector2(x, y);
        //        }

        /// <summary>
        /// Draws the TextSprite to the CurrentRenderTarget
        /// </summary>
        ///

        #endregion Constructors

        #region Methods

        public void Draw()
        {
            _textSprite.Position = new Vector2f(Position.X, Position.Y); // -(_textSprite.GetLocalBounds().Height/2f));
            _textSprite.Color = Color;
            CluwneLib.CurrentRenderTarget.Draw(_textSprite);

            if (CluwneLib.Debug.DebugTextboxes)//CluwneLib.Debug()
            {
                FloatRect fr = _textSprite.GetGlobalBounds();
                CluwneLib.drawHollowRectangle((int)fr.Left, (int)fr.Top, (int)fr.Width, (int)fr.Height, 1.0f, Color.Red);
            }
        }

        /// <summary>
        /// Get the length, in pixels, that the provided string would be.
        /// </summary>
        public int MeasureLine(string _text)
        {
            string temp = Text;
            Text = _text;
            int value = (int)_textSprite.FindCharacterPos((uint)_textSprite.DisplayedString.Length + 1).X;
            Text = temp;
            return value;
        }

        /// <summary>
        /// Get the length, in pixels, of this TextSprite.
        /// </summary>
        public int MeasureLine()
        {
            return MeasureLine(Text);
        }

        public Vector2f FindCharacterPos(uint index)
        {
            return _textSprite.FindCharacterPos(index);
        }

        #endregion Methods

        #region Accessors

        public Vector2i Size;

        public Color Color;

        public Vector2f ShadowOffset { get; set; }

        public bool Shadowed
        {
            get => _shadowed;
            set => _shadowed = value;
        }

        public uint FontSize
        {
            get => _textSprite.CharacterSize;
            set => _textSprite.CharacterSize = value;
        }

        public Color ShadowColor
        {
            get => _shadowColor;
            set => this._shadowColor = value;
        }

        public string Text
        {
            get => _textSprite.DisplayedString;
            set => _textSprite.DisplayedString = value;
        }

        public Vector2i Position;

        public int Width
        {
            get
            {
                var a = _textSprite;
                var b = a.GetLocalBounds();
                var c = b.Width;
                var d = (int)c;
                return d;
            }
        }
        // FIXME take into account newlines.
        public int Height => (int)_textSprite.CharacterSize;
        #endregion Accessors
    }
}
