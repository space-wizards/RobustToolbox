using SFML.Graphics;
using SS14.Shared.Maths;
using System;
using Color = System.Drawing.Color;




namespace SS14.Client.Graphics.CluwneLib.Sprite
{
    /// <summary>
    /// Sprite that contains Text
    /// </summary>
    public class TextSprite : ICluwneDrawable
    {

        private Boolean _shadowed;                                 // Is the Text Shadowed
        private Color _shadowColor;                                // Shadow Color
        private Text _textSprite;

        #region Constructors

        /// <summary>
        /// Creates a TextSprite
        /// </summary>
        /// <param name=""> Label of TextSprite </param>
        /// <param name="p2">   </param>
        /// <param name="font"> Font to use when displaying Text </param>
        public TextSprite( string Label, string text, Font font )
        {
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
        #endregion

        #region Methods
        public void Draw (RenderTarget target)
        {

            if (CluwneLib.Debug.Fontsize > 0)
                _textSprite.CharacterSize=CluwneLib.Debug.Fontsize;
            _textSprite.Position = new Vector2(Position.X, Position.Y); // -(_textSprite.GetLocalBounds().Height/2f));
            _textSprite.Color = CluwneLib.SystemColorToSFML(Color);
            target.Draw(_textSprite);

            if (CluwneLib.Debug.TextBorders) {
                FloatRect fr = _textSprite.GetGlobalBounds();
                CluwneLib.drawHollowRectangle((int)fr.Left, (int) fr.Top, (int) fr.Width, (int) fr.Height, 1.0f, Color.Red);
            }
        }

        public void Draw() {
            Draw(CluwneLib.CurrentRenderTarget);
        }


        public int MeasureLine(string _text)
        {
            return _text.Length;
        }
        #endregion

        #region Accessors

        public System.Drawing.Size Size;

        public System.Drawing.Color Color;

        public Vector2 ShadowOffset { get; set; }

        public Boolean Shadowed
        {
            get
            {
                return _shadowed;
            }
            set
            {
                this._shadowed = value;
            }
        }

        public uint FontSize {
            get { return _textSprite.CharacterSize; }
            set { _textSprite.CharacterSize = value; }
        }

        public Color ShadowColor
        {
            get
            {
                return _shadowColor;
            }
            set
            {
                this._shadowColor = value;
            }
        }

        public string Text
        {
            get
            {
                return _textSprite.DisplayedString;
            }
            set
            {
                _textSprite.DisplayedString = value;
            }
        }
    

        public Vector2 Position;

        public int Width
        {
            get { return (int) _textSprite.GetLocalBounds().Width; }

        }

        public int Height
        {
            // FIXME take into account newlines.
            get { return (int) _textSprite.CharacterSize; }
        }

        #endregion
    }
}
