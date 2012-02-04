using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.GUI;
using ClientResourceManager;

using Lidgren.Network;
using SS13_Shared;

namespace SS13.UserInterface
{
    class JobSelectButton : GuiComponent
    {
        Sprite Button;
        Sprite Job;

        public TextSprite labelDesc;

        public delegate void JobButtonPressHandler(JobSelectButton sender);
        public event JobButtonPressHandler Clicked;
        private Rectangle buttonArea;

        public bool selected = false;
        public bool available = true;

        public JobSelectButton(string text, string jobIcon , string desc)
            : base()
        {
            Button = ResMgr.Singleton.GetSprite("job_button");
            Job = ResMgr.Singleton.GetSprite(jobIcon);

            labelDesc = new TextSprite("JobButtonDescLabel" + text, text + ":\n" + desc, ResMgr.Singleton.GetFont("CALIBRI"));
            labelDesc.Color = System.Drawing.Color.Black;
            labelDesc.ShadowColor = System.Drawing.Color.DimGray;
            labelDesc.Shadowed = true;
            labelDesc.ShadowOffset = new Vector2D(1, 1);

            Update();
        }

        public override void Update()
        {
            buttonArea = new Rectangle(new Point(this.position.X, this.position.Y), new Size((int)Button.Width, (int)Button.Height));
            clientArea = new Rectangle(new Point(this.position.X, this.position.Y), new Size((int)Button.Width + (int)labelDesc.Width + 2, (int)Button.Height));
            labelDesc.Position = new Point(buttonArea.Right + 2, buttonArea.Top);
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
            Button.Draw(buttonArea);
            Job.Draw(buttonArea);
            labelDesc.Draw();
            Button.Color = System.Drawing.Color.White;

        }

        public override void Dispose()
        {
            labelDesc = null;
            Button = null;
            Job = null;
            Clicked = null;
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (!available) return false;
            if (buttonArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                if (Clicked != null) Clicked(this);
                selected = true;
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