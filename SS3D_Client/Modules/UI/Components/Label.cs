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
    class Label : GuiComponent
    {
        public TextSprite Text { get; private set; }

        public delegate void LabelPressHandler(Label sender);
        public event LabelPressHandler Clicked;

        public bool drawBorder = false;
        public bool drawBackground = false;

        public System.Drawing.Color borderColor = System.Drawing.Color.Black;
        public System.Drawing.Color backgroundColor = System.Drawing.Color.Gray;

        public Label(string text)
            : base()
        {
            Text = new TextSprite("Label" + text, text, ResMgr.Singleton.GetFont("CALIBRI"));
            Text.Color = System.Drawing.Color.Black;
            Update();
        }

        public override void Update()
        {
            Text.Position = position;
            ClientArea = new Rectangle(this.position, new Size((int)Text.Width, (int)Text.Height));
        }

        public override void Render()
        {
            if (drawBackground) Gorgon.Screen.FilledRectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, backgroundColor);
            if (drawBorder) Gorgon.Screen.Rectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, borderColor);
            Text.Draw();
        }

        public override void Dispose()
        {
            Text = null;
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
