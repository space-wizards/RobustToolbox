using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using ClientServices.Resources;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.GUI;
using SS13.UserInterface;
using Lidgren.Network;
using SS13_Shared;

namespace SS13.UserInterface
{
    class Progress_Bar : GuiComponent
    {
        public TextSprite Text { get; protected set; }

        public System.Drawing.Color borderColor = System.Drawing.Color.Black;
        public System.Drawing.Color backgroundColor = System.Drawing.Color.SteelBlue;
        public System.Drawing.Color barColor = System.Drawing.Color.LightSteelBlue;

        protected Size size;

        protected float val = 0;

        public virtual float Value 
        {
            get { return val; }
            set { val = Math.Min(Math.Max(value, min), max); } 
        }

        protected float min = 0;
        protected float max = 1000;

        protected float percent = 0;

        public Progress_Bar(Size _size)
            : base()
        {
            Text = new TextSprite("ProgressBarText", "", ResourceManager.GetFont("CALIBRI"));
            Text.Color = Color.Black;
            Text.ShadowColor = Color.DimGray;
            Text.ShadowOffset = new Vector2D(1,1);
            Text.Shadowed = true;

            size = _size;

            Update();
        }

        public override void Update()
        {
            Text.Text = Math.Round(percent * 100).ToString() + "%";
            Text.Position = new Vector2D(position.X + (size.Width / 2f - Text.Width / 2f), position.Y + (size.Height / 2f - Text.Height / 2f));
            ClientArea = new Rectangle(this.position, size);
            Value++;
        }

        public override void Render()
        {
            percent = (float)(val - min) / (float)(max - min);
            float barWidth = (float)size.Width * percent;

            Gorgon.Screen.FilledRectangle(clientArea.X, clientArea.Y, clientArea.Width, clientArea.Height, backgroundColor);
            Gorgon.Screen.FilledRectangle(clientArea.X, clientArea.Y, barWidth, clientArea.Height, barColor);
            Gorgon.Screen.Rectangle(clientArea.X, clientArea.Y, clientArea.Width, clientArea.Height, borderColor);

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
