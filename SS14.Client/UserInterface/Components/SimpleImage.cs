using SS14.Client.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Graphics.Utility;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.Components
{
    public class SimpleImage : GuiComponent
    {
        private readonly IResourceCache _resourceCache; //TODO Make simpleimagebutton and other ui classes use this.

        private Sprite drawingSprite;

        public SimpleImage()
        {
            _resourceCache = IoCManager.Resolve<IResourceCache>();
            Update(0);
        }

        public string Sprite
        {
            set { drawingSprite = _resourceCache.GetSprite(value); Update(0); }
        }

        public Color Color
        {
            get { return (drawingSprite != null ? drawingSprite.Color : Color.White); }
            set { drawingSprite.Color = value; }
        }

        public override void Update(float frameTime)
        {
            if (drawingSprite != null)
            {
                var bounds = drawingSprite.LocalBounds;
                ClientArea = Box2i.FromDimensions(Position, new Vector2i((int)bounds.Width, (int)bounds.Height));
            }
        }

        public override void Render()
        {
            drawingSprite.Draw(CluwneLib.CurrentRenderTarget, new RenderStates(BlendMode.Alpha));
        }

        public override void Dispose()
        {
            drawingSprite = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override Box2i ClientArea
        {
            get
            {
                var fr = drawingSprite.LocalBounds;
                return Box2i.FromDimensions((int)drawingSprite.Position.X, (int)drawingSprite.Position.Y, (int)fr.Width, (int)fr.Height);
            }
        }

        public override Vector2i Position
        {
            get
            {
                if (drawingSprite == null)
                    return Vector2i.Zero;
                return (Vector2i)drawingSprite.Position;
            }
            set => drawingSprite.Position = value;
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            return false;
        }
    }
}
