using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Utility;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
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
            /*
            if (drawingSprite != null)
            {
                var bounds = drawingSprite.GetLocalBounds();
                ClientArea = Box2i.FromDimensions(Position, new Vector2i((int)bounds.Width, (int)bounds.Height));
            }
            */
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

        /*
        public override Box2i ClientArea {
            get {
                var fr = drawingSprite.GetLocalBounds().Convert();
                return Box2i.FromDimensions((int)drawingSprite.Position.X, (int)drawingSprite.Position.Y, (int)fr.Width, (int)fr.Height);
            }
        }
        */
        public override void Resize()
        {
            var fr = drawingSprite.GetLocalBounds().Convert();
            _size = new Vector2i((int) fr.Width, (int) fr.Height);
            _clientArea = Box2i.FromDimensions(Position.X, Position.Y, _size.X, _size.Y);

            base.Resize();

            if (drawingSprite != null)
            {
                drawingSprite.Position = new Vector2f(_screenPos.X, _screenPos.Y);
            }
        }
    }
}
