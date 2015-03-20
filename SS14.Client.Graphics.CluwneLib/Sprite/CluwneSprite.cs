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

namespace SS14.Client.Graphics.CluwneLib.Sprite
{
    public class CluwneSprite : BaseSprite
    {
        private Drawing.RectangleF _AABB;
        private RenderTarget _target;

        private BlendMode _blendingMode = BlendMode.None;

        public string Name;
        private string key;
        private Image image;
        private Vector2 _imageOffset;

        public CluwneSprite()
        {
        }

        public CluwneSprite(RenderTarget target)
        {
            _target = target;
        }

        public CluwneSprite(Texture texture)
            : base(texture)
        {
        }

        public CluwneSprite(Texture texture, IntRect rectangle)
            : base(texture, rectangle) 
        {
        }

        public CluwneSprite(BaseSprite copy)
            : base(copy)
        {
        }

        public CluwneSprite(string key, Image image)
        {
            // TODO: Complete member initialization
            this.key = key;
            this.image = image;
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

        private void UpdateAABB()
        {
            throw new NotImplementedException();
        }

        public bool IsAABBUpdated { get; set; }
        public bool HorizontalFlip { get; set; }

        public RenderTarget RenderTarget
        {
            get
            {
                if (_target == null)
                    _target = CluwneLib.Screen;
                return _target;
            }
            set { _target = value; }
        }

        public void SetPosition(float width, float height)
        {
            throw new NotImplementedException();
        }

        public void Draw()
        {
          
        }

        public void Draw (CluwneSprite CS1 )
        {

        }

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

        public void Draw ( Drawing.Rectangle _clientAreaMain )
        {
            throw new NotImplementedException();
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
            get { return image; }
            set { }
        }

        public Vector2 Size { get; set; }


        public Color Color
        {
            get { return Color; }
            set { Color = value; }
        }
    }
}
