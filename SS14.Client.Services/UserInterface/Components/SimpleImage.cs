using SS14.Client.Graphics.CluwneLib.Sprite;
using SS14.Shared.Maths;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.IoC;
using SFML.Window;
using System;
using System.Drawing;


namespace SS14.Client.Services.UserInterface.Components
{
    public class SimpleImage : GuiComponent
    {
        private readonly IResourceManager _resourceManager; //TODO Make simpleimagebutton and other ui classes use this.

		private CluwneSprite drawingSprite;

        public Vector2 size;

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

        public Color Color
        {
            get { return drawingSprite != null ? drawingSprite.Color : Color.White; }
            set { drawingSprite.Color = value; }
        }

        public BlendingMode BlendingMode
        {
            get { return drawingSprite != null ? drawingSprite.BlendingMode : BlendingMode.None; }
            set { drawingSprite.BlendingMode = value; }
        }

        public override void Update(float frameTime)
        {
            size = drawingSprite != null ? drawingSprite.Size : Vector2.Zero;
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