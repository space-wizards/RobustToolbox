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
    class Button : GuiComponent
    {
        GUIElement ButtonMain;
        GUIElement ButtonLeft;
        GUIElement ButtonRight;     

        public TextSprite label;

        public delegate void ButtonPressHandler(Button sender);
        public event ButtonPressHandler Clicked;

        private Rectangle clientAreaMain; //Having 3 is ugly. Will fix later.
        private Rectangle clientAreaLeft;
        private Rectangle clientAreaRight;

        public Size Size {get; private set;}

        public object UserData;

        public Button(string text)
            : base()
        {
            ButtonLeft = UiManager.Singleton.Skin.Elements["Controls.Button.Left"];
            ButtonMain = UiManager.Singleton.Skin.Elements["Controls.Button.Body"];
            ButtonRight = UiManager.Singleton.Skin.Elements["Controls.Button.Right"];

            label = new TextSprite("ButtonLabel" + text, text, ResMgr.Singleton.GetFont("CALIBRI"));
            label.Color = System.Drawing.Color.Black;

            Update();
        }

        public override void Update()
        {
            clientArea = new Rectangle(this.position, new Size(ButtonLeft.Dimensions.Width + ButtonMain.Dimensions.Width + ButtonRight.Dimensions.Width, ButtonMain.Dimensions.Height));
            clientAreaLeft = new Rectangle(this.position, new Size(ButtonLeft.Dimensions.Width, ButtonLeft.Dimensions.Height));
            clientAreaMain = new Rectangle(new Point(clientAreaLeft.Right, this.position.Y), new Size((int)label.Width, ButtonMain.Dimensions.Height));
            label.Position = new Point(clientAreaLeft.Right, this.position.Y);
            clientAreaRight = new Rectangle(new Point(clientAreaMain.Right, this.position.Y), new Size(ButtonRight.Dimensions.Width, ButtonRight.Dimensions.Height));
            Size = new Size(clientAreaLeft.Width + clientAreaMain.Width + clientAreaRight.Width, clientAreaMain.Height);
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
            ButtonLeft = null;
            ButtonMain = null;
            ButtonRight = null;
            Clicked = null;
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
