using SFML.Graphics;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BaseSprite = SFML.Graphics.Sprite;
using Color = System.Drawing.Color;




namespace SS14.Client.Graphics.CluwneLib.Sprite
{
    /// <summary>
    /// Sprite that contains Text
    /// </summary>
    public class TextSprite : BaseSprite
    {

        private string _key;
        private string _text;                                      // Text Displayed on screen
        private int _width;                                        // Width of the Text Sprite
        private int _height;                                       // Height of the Text Sprite    
        private Boolean _shadowed;                                 // Is the Text Shadowed
        private Vector2 _position;                                 // Position (X , Y)  Of Text Sprite
        private Font _font;                                        // Font of Text Displayed
        private Color _color;                                      // Base Color 
        private Color _shadowColor;                                // Shadow Color
        private Text _textSprite;
        private string _label;                        


        #region Constructors
        /// <summary>
        /// Default Constructor, creates a empty TextSprite
        /// </summary>
        public TextSprite ()
        {

        }

        /// <summary>
        /// Creates a TextSprite
        /// </summary>
        /// <param name="SPRITEID"> ID of the TextSprite </param>
        /// <param name="Label">  Label of TextSprite </param>
        /// <param name="font"> Font to use when displaying Text </param>
        public TextSprite( string SPRITEID, string Label, Font font )
        {
            this._key = SPRITEID;
            this._label = Label;
            this._font = font;
        }

        /// <summary>
        /// Creates a TextSprite
        /// </summary> 
        /// <param name="SPRITEID"> ID of the TextSprite</param>
        /// <param name="x"> position X of TextSprite </param>
        /// <param name="y"> Position Y of TextSprite </param>
        /// <param name="width"> Width of TextSprite </param>
        /// <param name="height"> Height of TextSprite </param>
        public TextSprite(string SPRITEID, int x, int y, int width, int height)
        {
            this._key = SPRITEID;
            this._position = new Vector2(x, y);
           //width, height code goes here

            
        }
        
        /// <summary>
        /// Draws the TextSprite to the CurrentRenderTarget
        /// </summary>
        /// 
        #endregion

        #region Methods
        public void Draw ( )
        {
            _textSprite = new Text();
            _textSprite.Color = CluwneLib.SystemColorToSFML(_color);
            _textSprite.DisplayedString = _text;
            _textSprite.Position = _position;
            _textSprite.Font = _font;
           
            CluwneLib.CurrentRenderTarget.Draw(_textSprite);
        }


        public int MeasureLine(string _text)
        {
            return _text.Length;
        }
        #endregion

        #region Accessors

        public Color Color
        {
            get
            {
                return _color;
            }
            set
            {
                this._color = value;
            }


        }

        public System.Drawing.Size Size { get; set; }

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
                return _text;
            }
            set
            {
                _text = value;
            }

            
        }
    

        public Vector2 Position 
        {
            get
            {
                return _position;
            } 
            set
            {
                this._position = value;
            }
        }

        public int Width
        {
            get
            {
                return _width;
            }
            set
            {
                this._width = value;
            }

        }

        public int Height
        {
            get;
            set;
        }

        #endregion


     
     
    }
}
