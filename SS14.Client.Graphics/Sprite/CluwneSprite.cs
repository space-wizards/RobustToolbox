using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SColor = System.Drawing.Color;
using SFML.Graphics;
using Drawing = System.Drawing;
using BaseSprite = SFML.Graphics.Sprite;
using Image = SFML.Graphics.Image;
using SS14.Client.Graphics.Sprite;
using SS14.Shared.Maths;
using SS14.Client.Graphics.Render;
using System.Drawing;
using SFML.System;
using System.Diagnostics;
using SS14.Client.Graphics.Shader;

namespace SS14.Client.Graphics.Sprite
{
    [DebuggerDisplay("[CluwneSprite] Key: {key} | X: {Position.X} Y: {Position.Y} W: {Size.X} H: {Size.Y} SX: {Scale.X} SY: {Scale.Y} | RenderTarget = {renderTarget} ")]
    public class CluwneSprite : BaseSprite, ICluwneDrawable
    {     
      
        private string key;
        private RectangleF _AABB;
        private Vector2 size;
      
        #region Accessors     

        public float Width
        {
            get { return GetLocalBounds().Width; }
        }

        public float Height
        {
            get { return GetLocalBounds().Height; }
        }

        public float X
        {
            get { return GetLocalBounds().Left; }
        }

        public float Y
        {
            get { return GetLocalBounds().Top; }
        }

        public string Key
        {
            get
            {
                return key;
            }
            private set
            {
                CheckIfKeyIsNull(value);               
                key = value;
            }
        }

        public bool Smoothing
        {
            get 
            {
                return Texture.Smooth; 
            }
            set 
            { 
                Texture.Smooth = value; 
            }
        }

        public bool IsAABBUpdated = true;

        public bool HorizontalFlip;
       
        public RenderTarget renderTarget
        {
            get;
            private set;
        }

        public BlitterSizeMode Mode
        {
            get;
            set;
        }

        public BlendMode BlendingMode
        {
            get;
            set;
        }

        public Texture Texture
        {
            get { return base.Texture; }
            set { base.Texture = value; }
        }

        public Vector2 ImageOffset
        {
            get;
            set;
        }

        public Vector2 Position
        {
            get { return base.Position; }
            set { base.Position = value; }
        }

        public Vector2 Size
        {
            get { return size; }
            set { size = value; }
        }

        public Vector2 Scale
        {
            get { return base.Scale;}
            set { base.Scale = value; }
        }       

        public RectangleF AABB 
        {
            get
            {
                if (IsAABBUpdated)
                    UpdateAABB();
                return _AABB;
            }
        }

        public bool DepthWriteEnabled 
        {
            get;
            set;
        }

        #endregion

        #region Constructors
        
        /// <summary>
        /// Creates a new CluwneSprite with a key and a specific renderTarget
        /// </summary>
        /// <param name="key"> key </param>
        /// <param name="_renderImage"> RenderTarget to use </param>
        public CluwneSprite(string key, RenderTarget target)
        {           
            Key = key;          
            renderTarget = target;
            Position = new Vector2(X, Y);
            Size = new Vector2(Width, Height);
        }

        /// <summary>
        /// Creates a new CluwneSprite with a key and a specific renderTarget
        /// </summary>
        /// <param name="key"> key </param>
        /// <param name="_renderImage"> RenderTarget to use </param>
        public CluwneSprite(string key, RenderImage target)
        {
            Key = key;           
            renderTarget = target;
            Position = new Vector2(X,Y);
            Size = new Vector2(Width, Height);
        }

        /// <summary>
        /// Creates a new CluwneSprite With a key and a texture
        /// </summary>
        /// <param name="key"> key </param>
        /// <param name="image"> Image </param>
        public CluwneSprite(string key, Texture texture) : base (texture)
        {           
            Key = key;           
            Position = new Vector2(X, Y);
            Size = new Vector2(Width, Height);
        }

        /// <summary>
        /// Creates a new CluwneSprite with a specified portion of the Texture
        /// </summary>
        /// <param name="texture"> Texture to draw </param>
        /// <param name="rectangle"> What part of the Texture to use </param>
        public CluwneSprite(string key, Texture texture, IntRect rectangle)  : base(texture,rectangle)
        {
            Key = key;           
            Position = new Vector2(X, Y);
            Size = new Vector2(Width, Height);
        }

        /// <summary>
        /// Creates a new CluwneSprite from a copy of a sprite
        /// </summary>
        /// <param name="copy"> Sprite to copy </param>
        public CluwneSprite(CluwneSprite copy) : base(copy)
        {
           Key = copy.Key + "Copy";         
           Position = new Vector2(X, Y);
           Size = new Vector2(Width, Height);
        }
        #endregion

        #region Helper Methods

        /// <summary>
        /// Enforce no null keys
        /// </summary>
        /// <param name="key"></param>
        private void CheckIfKeyIsNull(string key)
        {
            if (key.Equals(null))
            {
                throw new Exception("key Cannot be null!");
            }
        }

        /// <summary>
        /// Update Boundaries
        /// </summary>
        private void UpdateAABB()
        {
            IntRect _fr = (IntRect) GetGlobalBounds();
            _AABB = new RectangleF(new PointF(_fr.Left, _fr.Top), new SizeF(_fr.Width, _fr.Height));
        }
       
        /// <summary>
        /// sets the position of the Sprite
        /// </summary>
        /// <param name="X"> X Pos </param>
        /// <param name="Y"> Y Pos </param>
        public void SetPosition(float X, float Y)
        {
            Position = new Vector2(X, Y);
        }

        #endregion

        #region Draw methods

        /// <summary>
        /// Scale the sprite
        /// </summary>
        /// <param name="rect"> Rectangle of new scale / size </param>
        public void Draw(IntRect rect)
        {        
            Scale = new Vector2((float)rect.Width / (float)TextureRect.Width, (float)rect.Height / (float)TextureRect.Height);
            Draw();
        }

        /// <summary>
        /// Scale the sprite
        /// </summary>
        /// <param name="rect"> new Scale </param>
        public void Draw(Rectangle rect)
        {           
            Scale = new Vector2f((float)rect.Width / (float)TextureRect.Width, (float)rect.Height / (float)TextureRect.Height);
            Draw();
        }

        /// <summary>
        /// Draw to a specific RenderImage
        /// </summary>
        /// <param name="target"> RenderImage to draw to </param>
        public void DrawTo(RenderImage target)
        {
            renderTarget = target;
            Draw();
        }

        /// <summary>
        /// Draws the sprite to the current renderTarget
        /// </summary>
        public void Draw()
        {
            RenderStates states = new RenderStates(CluwneLib.CurrentShader);
            

            if (CluwneLib.CurrentShader == null)
            {
                if
                    (renderTarget != null) 
                    renderTarget.Draw(this);
                else
                    CluwneLib.CurrentRenderTarget.Draw(this);
            }
            else
            {
                if (renderTarget != null)
                    renderTarget.Draw(this, states);
                else
                    CluwneLib.CurrentRenderTarget.Draw(this,states);
            }


            if (CluwneLib.Debug.RenderingDelay > 0) 
            {
                CluwneLib.Screen.Display();
                System.Threading.Thread.Sleep(CluwneLib.Debug.RenderingDelay);
            }
        }

        /// <summary>
        /// Draw Sprite to screen with a shader
        /// </summary>
        /// <param name="shader"> Shader to use when drawing </param>
        public void Draw(GLSLShader shader)
        {
            RenderStates states = new RenderStates(shader);
  
            
            if (renderTarget != null)
                renderTarget.Draw(this, states);
            else
                CluwneLib.CurrentRenderTarget.Draw(this, states);
            


            if (CluwneLib.Debug.RenderingDelay > 0)
            {
                CluwneLib.Screen.Display();
                System.Threading.Thread.Sleep(CluwneLib.Debug.RenderingDelay);
            }
        }

        #endregion
    }
}
