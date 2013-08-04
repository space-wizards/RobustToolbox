using System;
using System.Drawing;
using ClientInterfaces.Resource;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientServices.UserInterface.Components
{
    internal class Progress_Bar : GuiComponent
    {
        private readonly IResourceManager _resourceManager;
        protected Size Size;

        public Color backgroundColor = Color.SteelBlue;
        public Color barColor = Color.LightSteelBlue;
        public Color borderColor = Color.Black;

        protected float max = 1000;
        protected float min = 0;

        protected float percent = 0;
        protected float val = 0;

        public Progress_Bar(Size size, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            Text = new TextSprite("ProgressBarText", "", _resourceManager.GetFont("CALIBRI"));
            Text.Color = Color.Black;
            Text.ShadowColor = Color.DimGray;
            Text.ShadowOffset = new Vector2D(1, 1);
            Text.Shadowed = true;

            Size = size;

            Update(0);
        }

        public TextSprite Text { get; protected set; }

        public virtual float Value
        {
            get { return val; }
            set { val = Math.Min(Math.Max(value, min), max); }
        }

        public override void Update(float frameTime)
        {
            Text.Text = Math.Round(percent*100).ToString() + "%";
            Text.Position = new Vector2D(Position.X + (Size.Width/2f - Text.Width/2f),
                                         Position.Y + (Size.Height/2f - Text.Height/2f));
            ClientArea = new Rectangle(Position, Size);
            Value++;
        }

        public override void Render()
        {
            percent = (val - min)/(max - min);
            float barWidth = Size.Width*percent;

            Gorgon.CurrentRenderTarget.FilledRectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height,
                                                       backgroundColor);
            Gorgon.CurrentRenderTarget.FilledRectangle(ClientArea.X, ClientArea.Y, barWidth, ClientArea.Height, barColor);
            Gorgon.CurrentRenderTarget.Rectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height,
                                                 borderColor);

            Text.Draw();
        }

        public override void Dispose()
        {
            Text = null;
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