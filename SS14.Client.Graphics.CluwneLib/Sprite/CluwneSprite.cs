using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Graphics;
using Drawing = System.Drawing;
using BaseSprite = SFML.Graphics.Sprite;

namespace SS14.Client.Graphics.CluwneLib.Sprite
{
    public class CluwneSprite : BaseSprite
    {
        private Drawing.RectangleF _AABB;
        private RenderTarget _target;
        private BlendingMode _blendingMode = BlendingMode.None;

        public string Name;

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
            throw new NotImplementedException();
        }

        public float Width
        {
            get { return GetLocalBounds().Width; }
        }

        public float Height
        {
            get { return GetLocalBounds().Height; }
        }

        public BlendingMode BlendingMode
        {
            get { return _blendingMode; }
            set { _blendingMode = value; }
        }
    }
}
