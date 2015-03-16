using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;




namespace SS14.Client.Graphics.CluwneLib.Sprite
{
    /// <summary>
    /// Handles Text Sprites
    /// </summary>
    public class TextSprite
    {
        
        private string p1;                                         // Title 
        private string p2;
        private string _text; 
        private int _Width;                                        // Width of the Text Sprite
        private int _Height;                                       // Height of the Text Sprite    
        private Boolean _Shadowed;                                 // Is the Text Shadowed
        private Vector2 _Position;                                 // Position (X , Y)  Of Text Sprite
        private SFML.Graphics.Font _Font;                          // Text Font
        private System.Drawing.Color _baseColor;                   // Base Color 
        private System.Drawing.Color _shadowColor;                 // Shadow Color

       
        public TextSprite ()
        {

        }

        public TextSprite( string p1 , string p2 , SFML.Graphics.Font font )
        {
            //TODO FINISH THIS
            this.p1 = p1;
            this.p2 = p2;
            this._Font = font;
        }


        public void Draw ( )
        {
            //TODO Draw Sprite
        }





        #region Accessors
        
        public System.Drawing.Color Color
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

        public System.Drawing.Color ShadowColor
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

    }
}
