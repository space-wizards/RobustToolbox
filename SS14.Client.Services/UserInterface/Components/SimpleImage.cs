using SS14.Client.Graphics.Sprite;
using SS14.Shared.Maths;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.IoC;
using SFML.Window;
using SFML.Graphics;
using System;
using Color = System.Drawing.Color;
using System.Drawing;
using SS14.Client.Graphics;


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
            set { drawingSprite = _resourceManager.GetSprite(value); Update(0); }
        }

        public Color Color
        {
            get { return (drawingSprite != null ? System.Drawing.Color.White : System.Drawing.Color.White); }
            set { drawingSprite.Color = CluwneLib.SystemColorToSFML (value); }
        }

        public BlendMode BlendingMode
        {
            get { return drawingSprite != null ? drawingSprite.BlendingMode : drawingSprite.BlendingMode; }
            set { drawingSprite.BlendingMode = value;}
        }

        public override void Update(float frameTime)
        {
            size = drawingSprite != null ? drawingSprite.Size : Vector2.Zero;
            ClientArea = new Rectangle(Position, new Size((int) size.X, (int) size.Y));
        }

        public override void Render()
        {
            drawingSprite.Draw();
        }

        public override void Dispose()
        {
            drawingSprite = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override Rectangle ClientArea {
            get {
                FloatRect fr = drawingSprite.GetLocalBounds ();
                return new Rectangle ((int)drawingSprite.Position.X, (int)drawingSprite.Position.Y, (int)fr.Width, (int)fr.Height);
            }
        }

        public override Point Position {
            get {
                if (drawingSprite == null)
                    return new Point (0, 0);
                return new Point ((int)drawingSprite.Position.X, (int)drawingSprite.Position.Y);
            }
            set { drawingSprite.Position = new SFML.System.Vector2f (value.X, value.Y); }
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