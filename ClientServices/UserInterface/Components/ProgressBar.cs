using System;
using System.Drawing;
using ClientInterfaces;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientServices.UserInterface.Components
{
    class Progress_Bar : GuiComponent
    {
        private readonly IResourceManager _resourceManager;

        public TextSprite Text { get; protected set; }

        public System.Drawing.Color borderColor = System.Drawing.Color.Black;
        public System.Drawing.Color backgroundColor = System.Drawing.Color.SteelBlue;
        public System.Drawing.Color barColor = System.Drawing.Color.LightSteelBlue;

        protected Size Size;

        protected float val = 0;

        public virtual float Value 
        {
            get { return val; }
            set { val = Math.Min(Math.Max(value, min), max); } 
        }

        protected float min = 0;
        protected float max = 1000;

        protected float percent = 0;

        public Progress_Bar(Size size, IResourceManager resourceManager)
            : base()
        {
            _resourceManager = resourceManager;
            Text = new TextSprite("ProgressBarText", "", _resourceManager.GetFont("CALIBRI"));
            Text.Color = Color.Black;
            Text.ShadowColor = Color.DimGray;
            Text.ShadowOffset = new Vector2D(1,1);
            Text.Shadowed = true;

            Size = size;

            Update();
        }

        public override void Update()
        {
            Text.Text = Math.Round(percent * 100).ToString() + "%";
            Text.Position = new Vector2D(Position.X + (Size.Width / 2f - Text.Width / 2f), Position.Y + (Size.Height / 2f - Text.Height / 2f));
            ClientArea = new Rectangle(this.Position, Size);
            Value++;
        }

        public override void Render()
        {
            percent = (float)(val - min) / (float)(max - min);
            float barWidth = (float)Size.Width * percent;

            Gorgon.Screen.FilledRectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, backgroundColor);
            Gorgon.Screen.FilledRectangle(ClientArea.X, ClientArea.Y, barWidth, ClientArea.Height, barColor);
            Gorgon.Screen.Rectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, borderColor);

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
