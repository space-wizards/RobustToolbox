using System;
using System.Drawing;
using ClientInterfaces.Resource;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS13.IoC;

namespace ClientServices.UserInterface.Components
{
    public class SimpleImage : GuiComponent
    {
        private readonly IResourceManager _resourceManager; //TODO Make simpleimagebutton and other ui classes use this.

        private Sprite drawingSprite;

        public Vector2D size;

        public SimpleImage()
        {
            _resourceManager = IoCManager.Resolve<IResourceManager>();
            Update(0);
        }

        public string Sprite
        {
            get { return drawingSprite != null ? drawingSprite.Name : null; }
            set { drawingSprite = _resourceManager.GetSprite(value); }
        }

        public override void Update(float frameTime)
        {
            size = drawingSprite != null ? drawingSprite.Size : Vector2D.Zero;
            ClientArea = new Rectangle(Position, new Size((int) size.X, (int) size.Y));
        }

        public override void Render()
        {
            drawingSprite.Draw(ClientArea);
        }

        public override void Dispose()
        {
            drawingSprite = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }
    }
}