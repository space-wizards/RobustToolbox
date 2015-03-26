using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Color = SFML.Graphics.Color;
using SFML.Graphics;
using Drawing = System.Drawing;
using BaseSprite = SFML.Graphics.Sprite;
using Image = SFML.Graphics.Image;
using SS14.Client.Graphics.CluwneLib.Sprite;
using SS14.Shared.Maths;
using SS14.Client.Graphics.CluwneLib.Render;
using System.Drawing;

namespace SS14.Client.Graphics.CluwneLib.Sprite
{
    public class CluwneSprite : BaseSprite
    {
        private Drawing.RectangleF _AABB;
        private RenderTarget _target;
        private BlendMode _blendingMode = BlendMode.None;

        private string _key;
        private Image _image;
        private Vector2 _imageOffset;
        private RenderImage _renderTarget;
        private Vector2 _size;

        #region Constructors
 
        /// <summary>
        /// Creates a new CluwneSprite with a specified RenderTarget
        /// </summary>
        /// <param name="target"> Target to draw this sprite </param>
        public CluwneSprite(RenderTarget target) 
        {
            this._target = target;
        }

        /// <summary>
        /// Creates a new CluwneSprite with a specified Texture
        /// </summary>
        /// <param name="texture"></param>
        public CluwneSprite(Texture texture)  : base (texture)
        {
 
        }
        /// <summary>
        /// Creates a new CluwneSprite with a specified portion of the Texture
        /// </summary>
        /// <param name="texture"> Texture to draw </param>
        /// <param name="rectangle"> What part of the Texture to use </param>
        public CluwneSprite(string name, Texture texture, IntRect rectangle)  : base(texture,rectangle)
        {
            _key=name;
        }

        /// <summary>
        /// Creates a new CluwneSprite from a copy of a sprite
        /// </summary>
        /// <param name="copy"> Sprite to copy </param>
        public CluwneSprite(BaseSprite copy) : base(copy)
        {
         
        }
        /// <summary>
        /// Creates a new CluwneSprite With a key and a Image
        /// </summary>
        /// <param name="key"> Key </param>
        /// <param name="image"> Image </param>
        public CluwneSprite(string key, Image image)
        {       
            this._key = key;
            this._image = image;
        }

        /// <summary>
        /// Creates a new CluwneSprite with a key and a specific renderTarget
        /// </summary>
        /// <param name="key"> Key </param>
        /// <param name="_renderImage"> RenderTarget to use </param>
        public CluwneSprite(string key, RenderImage _renderImage) 
        {
            this._key = key;
            this._renderTarget = _renderImage;
        }

        #endregion


        private void UpdateAABB()
        {
            FloatRect _fr = GetLocalBounds();
            _AABB = new RectangleF(new PointF(_fr.Left, _fr.Top), new SizeF(_fr.Width, _fr.Height));
        }

       
        /// <summary>
        /// sets the position of the Sprite
        /// </summary>
        /// <param name="X"> X Pos </param>
        /// <param name="Y"> Y Pos </param>
        public void SetPosition(float X, float Y)
        {
            Vector2 temp = new Vector2(X, Y);
            base.Position = temp;
            
        }

        /// <summary>
        /// Draws this instance to the current renderTarget
        /// </summary>
        public void Draw()
        {
            if (_renderTarget != null)
                CluwneLib.CurrentRenderTarget = _renderTarget;

          CluwneLib.CurrentRenderTarget.Draw(this);
        }
        /// <summary>
        /// Draws a specific CluwneSprite to the current RenderTarget
        /// </summary>
        /// <param name="CS1"> CluwneSprite to draw </param>
        public void Draw (CluwneSprite CS1 )
        {
            if (_renderTarget != null)
                CluwneLib.CurrentRenderTarget = _renderTarget;

        }
        /// <summary>
        /// Draws a Rectangle 
        /// </summary>
        /// <param name="rect"> Rectangle to draw </param>
        public void Draw(Rectangle rect)
        {
            if (_renderTarget != null)
                CluwneLib.CurrentRenderTarget = _renderTarget;

            RectangleShape temp = new RectangleShape();
            temp.Position = new Vector2(rect.Location.X,rect.Location.Y);

            CluwneLib.CurrentRenderTarget.Draw(temp);
        }


        #region Accessors

        public string Name
        {
            get {return _key;}
            set { _key = value; }

        }
         
        public bool IsAABBUpdated = true;

        public bool HorizontalFlip;

        public float Width
        {
            get { return GetLocalBounds().Width; }
        }

        public float Height
        {
            get { return GetLocalBounds().Height; }
        }

        public BlendMode BlendingMode
        {
            get { return _blendingMode; }
          set { _blendingMode = value; }
        }

        public Vector2 ImageOffset
        {
            get { return _imageOffset; }
            set
            {
                _imageOffset = value;   
            }
        }

        public Image getImage
        {
            get { return _image; }
            set { _image = value;  }
        }

        public Vector2 Size { get { return _size; } set { _size = value; } } 

        public Drawing.RectangleF AABB
        {
            get
            {
                if (IsAABBUpdated)
                    UpdateAABB();
                return _AABB;
            }
        }

        public bool DepthWriteEnabled { get; set; }

        #endregion
    }
}
