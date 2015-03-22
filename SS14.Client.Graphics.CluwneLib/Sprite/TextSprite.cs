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
    /// Handles Text Sprites
    /// </summary>
    public class TextSprite : BaseSprite
    {
     
        private string _text; 
        private int _width;                                        // Width of the Text Sprite
        private int _height;                                       // Height of the Text Sprite    
        private Boolean _shadowed;                                 // Is the Text Shadowed
        private Vector2 _position;                                 // Position (X , Y)  Of Text Sprite
        private SFML.Graphics.Font _Font;                          // Text Font
        private Color _color;                                   // Base Color 
        private Color _shadowColor;                                 // Shadow Color
        private Text _textSprite;
       
        public TextSprite ()
        {

        }

        public TextSprite( string SPRITEID, string Label, Font font )
        {
           _textSprite = new Text();
          _textSprite.Font = font;
            
  

        }

        public TextSprite(string SPRITEID, int x, int y, int width, int height)
        {
           _textSprite = new Text();
            
        }
        
        public void Draw ( )
        {
            _textSprite.Color = CluwneLib.SystemColorToSFML(_color);
            _textSprite.DisplayedString = _text;
            _textSprite.Position = _position;

            CluwneLib.CurrentRenderTarget.Draw(_textSprite);
        }

        public int MeasureLine(string _text)
        {
            return _text.Length;
        }





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
