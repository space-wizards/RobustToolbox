using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Color = SFML.Graphics.Color;
using SFML.Graphics;
using Drawing = System.Drawing;
using BaseSprite = SFML.Graphics.Sprite;
using SS14.Client.Graphics.CluwneLib.Sprite;
using SS14.Shared.Maths;
using SS14.Client.Graphics.CluwneLib.Render;

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
        private Vector2 _position;
        private Vector2 _scale;
        private Vector2 _size;
        private float _rotation;
        private BaseSprite _baseSprite;
        private Texture _texture;
        private IntRect _textureRect;
        private Color _color;

       
        
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
        public CluwneSprite(Texture texture, IntRect rectangle)  : base(texture,rectangle)
        {
         
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
           
        }

       

        public void SetPosition(float width, float height)
        {
           
        }

        public void Draw()
        {
          CluwneLib.CurrentRenderTarget.Draw(this);
        }

        public void Draw (CluwneSprite CS1 )
        {

        }
        public void Draw(Drawing.Rectangle _clientAreaMain)
        {
           
        }


        #region Accessors

        public string Name
        {
            get {return _key;}
            set { _key = value; }

        }
         
        public bool IsAABBUpdated { get; set; }

        public bool HorizontalFlip { get; set; }

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

        public Color Color
        {
            get { return Color; }
            set { Color = value; }
        }

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
