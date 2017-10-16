using SFML.Graphics;
using SFML.System;
using OpenTK;
using OpenTK.Graphics;
using SS14.Client.Graphics.Utility;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.Graphics.Sprite
{
    /// <summary>
    /// Sprite that contains Text
    /// </summary>
    public class TextSprite
    {
        private bool _shadowed;            // Is the Text Shadowed
        private Color4 _shadowColor;       // Shadow Color
        private readonly Text _textSprite; // wrapped SFML Text object
        
        /// <summary>
        /// Creates a TextSprite
        /// </summary>
        /// <param name="text"> Text to display </param>
        /// <param name="font"> Font to use when displaying Text </param>
        /// <param name="size"> Size of the font to use </param>
        public TextSprite(string text, Font font, uint size)
        {
            _textSprite = new Text(text, font, size);
        }

        /// <summary>
        /// Creates a TextSprite
        /// </summary>
        /// <param name="text"> Text to display </param>
        /// <param name="font"> Font to use when displaying Text </param>
        public TextSprite(string text, Font font)
        {
            _textSprite = new Text(text, font, 14);
        }

        /// <summary>
        /// Draws the TextSprite to the CurrentRenderTarget
        /// </summary>
        ///
        
        public void Draw()
        {
            _textSprite.Position = new Vector2f(Position.X, Position.Y);
            _textSprite.FillColor = Color.Convert();
            CluwneLib.CurrentRenderTarget.Draw(_textSprite);

            if (CluwneLib.Debug.DebugTextboxes)
            {
                var fr = _textSprite.GetGlobalBounds().Convert();
                CluwneLib.drawHollowRectangle((int)fr.Left, (int)fr.Top, (int)fr.Width, (int)fr.Height, 1.0f, Color4.Red);
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

        public Vector2 FindCharacterPos(uint index)
        {
            return _textSprite.FindCharacterPos(index).Convert();
        }
        
        public Vector2i Size;

        public Color4 Color;

        public Vector2 ShadowOffset { get; set; }

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

        public Color4 ShadowColor
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
    }
}
