using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.UserInterface.Components
{
    public class SimpleImage : GuiComponent
    {
        private readonly IResourceManager _resourceManager; //TODO Make simpleimagebutton and other ui classes use this.

        private Sprite drawingSprite;

        public SimpleImage()
        {
            _resourceManager = IoCManager.Resolve<IResourceManager>();
            Update(0);
        }

        public string Sprite
        {
            //get { return drawingSprite != null ? drawingSprite.Key : null; }
            set { drawingSprite = _resourceManager.GetSprite(value); Update(0); }
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
                var bounds = drawingSprite.GetLocalBounds();
                ClientArea = new IntRect(Position, new Vector2i((int)bounds.Width, (int)bounds.Height));
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

        public override IntRect ClientArea {
            get {
                FloatRect fr = drawingSprite.GetLocalBounds();
                return new IntRect((int)drawingSprite.Position.X, (int)drawingSprite.Position.Y, (int)fr.Width, (int)fr.Height);
            }
        }

        public override Vector2i Position {
            get {
                if (drawingSprite == null)
                    return new Vector2i(0, 0);
                return new Vector2i((int)drawingSprite.Position.X, (int)drawingSprite.Position.Y);
            }
            set { drawingSprite.Position = new Vector2f (value.X, value.Y); }
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