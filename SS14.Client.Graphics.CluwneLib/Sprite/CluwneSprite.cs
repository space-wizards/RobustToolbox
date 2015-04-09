using SFML.Graphics;
using SS14.Client.Graphics.CluwneLib.Render;
using SS14.Shared.Maths;
using System.Drawing;
using BaseSprite = SFML.Graphics.Sprite;
using Drawing = System.Drawing;
using Image = SFML.Graphics.Image;

namespace SS14.Client.Graphics.CluwneLib.Sprite
{
    public class CluwneSprite : BaseSprite, ICluwneDrawable
    {
        private Drawing.RectangleF _AABB;
        private BlendMode _blendingMode = BlendMode.None;

        private string _key;
        private Image _image;
        private Vector2 _imageOffset;
        private RenderTarget _renderTarget;
        private Vector2 _size;

        #region Constructors
 
        /// <summary>
        /// Creates a new CluwneSprite with a specified RenderTarget
        /// </summary>
        /// <param name="target"> Target to draw this sprite </param>
        public CluwneSprite(RenderTarget target) 
        {
            this._renderTarget = target;
        }

        /// <summary>
        /// Creates a new CluwneSprite with a specified Texture
        /// </summary>
        /// <param name="texture"></param>
        public CluwneSprite(Texture texture)  : base (texture)
        {
 
        }
        public CluwneSprite(string name, Texture texture) : base(texture)
        {
            _key=name;
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
        public CluwneSprite(string key, RenderTarget target)
        {
            this._key = key;
            this._renderTarget = target;
        }

        public CluwneSprite(RenderImage image) {}

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
            if (this._key != null && this._key.Equals("_blit") && CluwneLib.Debug.RenderingDelay>0)
                return;
            if (_renderTarget != null && CluwneLib.Debug.RenderingDelay == 0)
                _renderTarget.Draw(this);
            else
                CluwneLib.CurrentRenderTarget.Draw(this);

            if (CluwneLib.Debug.RenderingDelay > 0) {
                CluwneLib.Screen.Display();
                System.Threading.Thread.Sleep(CluwneLib.Debug.RenderingDelay);
            }
        }
        /// <summary>
        /// Draws a specific CluwneSprite to the current RenderTarget
        /// </summary>
        /// <param name="CS1"> CluwneSprite to draw </param>
        public void Draw (CluwneSprite CS1 )
        {
            if (_renderTarget != null)
                _renderTarget.Draw(CS1);
            else
                CluwneLib.CurrentRenderTarget.Draw(CS1);
        }
        /// <summary>
        /// Draws a Rectangle 
        /// </summary>
        /// <param name="rect"> Rectangle to draw </param>
        public void Draw(Rectangle rect)
        {
            // scale the sprite to fit in the given rectangle.
            Vector2 oldScale=Scale;
            Vector2 oldPosition = base.Position;
            base.Position = new Vector2(rect.Left, rect.Top);
            Scale = new SFML.System.Vector2f( rect.Width / TextureRect.Width, rect.Height / TextureRect.Height );

            if (_renderTarget != null && CluwneLib.Debug.RenderingDelay == 0)
                _renderTarget.Draw(this);
            else
                CluwneLib.CurrentRenderTarget.Draw(this);
            base.Position = oldPosition;
            Scale=oldScale;
        }

        public void Draw(IntRect rect)
        {
            // scale the sprite to fit in the given rectangle.
            Vector2 oldScale=Scale;
            Vector2 oldPosition = base.Position;
            base.Position = new Vector2(rect.Left, rect.Top);
            Scale = new SFML.System.Vector2f( rect.Width / TextureRect.Width, rect.Height / TextureRect.Height );

            if (_renderTarget != null && CluwneLib.Debug.RenderingDelay == 0)
                _renderTarget.Draw(this);
            else
                CluwneLib.CurrentRenderTarget.Draw(this);
            base.Position = oldPosition;
            Scale=oldScale;
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
