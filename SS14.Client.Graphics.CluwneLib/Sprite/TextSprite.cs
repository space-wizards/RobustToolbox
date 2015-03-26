using SFML.Graphics;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Color = System.Drawing.Color;




namespace SS14.Client.Graphics.CluwneLib.Sprite
{
    /// <summary>
    /// Sprite that contains Text
    /// </summary>
    public class TextSprite
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
            _textSprite = new Text(text, font);
        }

        /// <summary>
        /// Creates a TextSprite
        /// </summary> 
        /// <param name="Label"> ID of the TextSprite</param>
        /// <param name="x"> position X of TextSprite </param>
        /// <param name="y"> Position Y of TextSprite </param>
        /// <param name="width"> Width of TextSprite </param>
        /// <param name="height"> Height of TextSprite </param>
        public TextSprite(string Label, int x, int y, int width, int height)
        {
            this._textSprite.Position = new Vector2(x, y);
        }

        /// <summary>
        /// Draws the TextSprite to the CurrentRenderTarget
        /// </summary>
        /// 
        #endregion

        #region Methods
        public void Draw ( )
        {
            CluwneLib.CurrentRenderTarget.Draw(_textSprite);
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
    

        new public Vector2 Position 
        {
            get
            {
                return _textSprite.Position;
            } 
            set
            {
                _textSprite.Position = value;
            }
        }

        public int Width
        {
            get { return (int) _textSprite.GetLocalBounds().Width; }

        }

        public int Height
        {
            get { return (int) _textSprite.GetLocalBounds().Height; }
        }

        #endregion
    }
}
