using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.GUI;
using SS3D.UserInterface;
using Lidgren.Network;
using SS3D_shared;
using ClientResourceManager;

namespace SS3D.UserInterface
{
    class Button : GuiComponent
    {
        Sprite ButtonMain;
        Sprite ButtonLeft;
        Sprite ButtonRight;     

        public TextSprite label;

        public delegate void ButtonPressHandler(Button sender);
        public event ButtonPressHandler Clicked;

        private Rectangle clientAreaMain; //Having 3 is ugly. Will fix later.
        private Rectangle clientAreaLeft;
        private Rectangle clientAreaRight;

        public Button(string text)
            : base()
        {
            ButtonLeft = ResMgr.Singleton.GetSprite("button_left");
            ButtonMain = ResMgr.Singleton.GetSprite("button_middle");
            ButtonRight = ResMgr.Singleton.GetSprite("button_right");

            label = new TextSprite("ButtonLabel" + text, text, ResMgr.Singleton.GetFont("CALIBRI"));
            label.Color = System.Drawing.Color.Black;

            Update();
        }

        public override void Update()
        {

            clientAreaLeft = new Rectangle(this.position, new Size((int)ButtonLeft.Width, (int)ButtonLeft.Height));
            clientAreaMain = new Rectangle(new Point(clientAreaLeft.Right, this.position.Y), new Size((int)label.Width, (int)ButtonMain.Height));
            clientAreaRight = new Rectangle(new Point(clientAreaMain.Right, this.position.Y), new Size((int)ButtonRight.Width, (int)ButtonRight.Height));
            clientArea = new Rectangle(this.position, new Size(clientAreaLeft.Width + clientAreaMain.Width + clientAreaRight.Width, clientAreaMain.Height));
            label.Position = new Point(clientAreaLeft.Right, this.position.Y + (int)(clientArea.Height / 2f) - (int)(label.Height / 2f));
        }

        public override void Render()
        {
            ButtonLeft.Draw(clientAreaLeft);
            ButtonMain.Draw(clientAreaMain);
            ButtonRight.Draw(clientAreaRight);
            label.Draw();
        }

        public override void Dispose()
        {
            label = null;
            ButtonLeft = null;
            ButtonMain = null;
            ButtonRight = null;
            Clicked = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
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
