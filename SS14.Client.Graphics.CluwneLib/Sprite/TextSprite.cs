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
        
        private string p1;                                         // Title 
        private string p2;
        private string _text; 
        private int _Width;                                        // Width of the Text Sprite
        private int _Height;                                       // Height of the Text Sprite    
        private Boolean _Shadowed;                                 // Is the Text Shadowed
        private Vector2 _Position;                                 // Position (X , Y)  Of Text Sprite
        private SFML.Graphics.Font _Font;                          // Text Font
        private Color _baseColor;                   // Base Color 
        private Color _shadowColor;                 // Shadow Color

       
        public TextSprite ()
        {

        }

        public TextSprite( string Name , string Text, Font font )
        {
           
        }

        public TextSprite(string Name, int x, int y, int width, int height)
        {



        }




        public void Draw ( )
        {
            //TODO Draw Sprite
        }





        #region Accessors
        
        public Color Color
        {
            get
            {
                return _baseColor;
            }
            set
            {
                this._baseColor = value;
            }


        }

        public Boolean Shadowed
        {
            get
            {
                return _Shadowed;
            }
            set
            {
                this._Shadowed = value;
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
                return _Position;
            } 
            set
            {
                this._Position = value;
            }
        }

        public int Width
        {
            get
            {
                return _Width;
            }
            set
            {
                this._Width = value;
            }

        }

        public int Height
        {
            get;
            set;
        }

        #endregion


        public int MeasureLine ( string _text )
        {
            throw new NotImplementedException();
        }

        public System.Drawing.Size Size
        {
            get;
            set;
        }


        public Vector2 ShadowOffset { get; set; }
    }
}
