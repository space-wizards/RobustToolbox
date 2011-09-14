using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.GUI;

using Lidgren.Network;
using SS3D_shared;

namespace SS3D.Modules.UI.Components
{
    class JobSelectButton : GuiComponent
    {
        GUIElement Button;
        GUIElement Job;

        public TextSprite label;

        public delegate void JobButtonPressHandler(JobSelectButton sender);
        public event JobButtonPressHandler Clicked;

        private Rectangle clientArea;

        public Size Size { get; private set; }

        public object UserData;

        public bool selected = false;
        public bool available = true;

        public JobSelectButton(string text, string jobIcon)
            : base()
        {
            Button = UiManager.Singleton.Skin.Elements["Controls.JobButton"];
            Job = UiManager.Singleton.Skin.Elements[jobIcon];

            label = new TextSprite("JobButtonLabel" + text, text, ResMgr.Singleton.GetFont("CALIBRI"));
            label.Color = System.Drawing.Color.Black;
            label.ShadowColor = System.Drawing.Color.DimGray;
            label.Shadowed = true;
            label.ShadowOffset = new Vector2D(1, 1);

            Update();
        }

        public override void Update()
        {
            clientArea = new Rectangle(new Point(this.position.X, this.position.Y), new Size(Button.Dimensions.Width, Button.Dimensions.Height));
            label.Position = new Point(clientArea.Left, clientArea.Bottom -2);
            Size = new Size(clientArea.Width, clientArea.Height + (int)label.Height);
        }

        public override void Render()
        {
            if (!available)
            {
                Button.Color = System.Drawing.Color.DarkRed;
            }
            else if (selected)
            {
                Button.Color = System.Drawing.Color.DarkSeaGreen;
            }
            Button.Draw(clientArea);
            Job.Draw(clientArea);
            label.Draw();
            Button.Color = System.Drawing.Color.White;
        }

        public override void Dispose()
        {
            Button = null;
            Job = null;
            Clicked = null;
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (!available) return false;
            if (clientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                if (Clicked != null) Clicked(this);
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }
    }
}