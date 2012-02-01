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
    class BlueprintButton : GuiComponent
    {
        Sprite icon; 
        public TextSprite label;

        public delegate void BlueprintButtonPressHandler(BlueprintButton sender);
        public event BlueprintButtonPressHandler Clicked;

        public string compo1;
        public string compo1Name;

        public string compo2;
        public string compo2Name;

        public string result;
        public string resultName;

        private Color bgcol = Color.Transparent;

        public BlueprintButton(string c1, string c1n, string c2, string c2n, string res, string resname)
            : base()
        {
            compo1 = c1;
            compo1Name = c1n;

            compo2 = c2;
            compo2Name = c2n;

            result = res;
            resultName = resname;

            icon = ResMgr.Singleton.GetSprite("blueprint");

            label = new TextSprite("blueprinttext", "", ResMgr.Singleton.GetFont("CALIBRI"));
            label.Color = Color.GhostWhite;

            label.ShadowColor = Color.DimGray;
            label.ShadowOffset = new Vector2D(1, 1);
            label.Shadowed = true;

            Update();
        }

        public override void Update()
        {
            clientArea = new Rectangle(this.position, new Size((int)(label.Width + icon.Width), (int)Math.Max(label.Height, icon.Height)));
            label.Position = new Point(position.X + (int)icon.Width, position.Y);
            icon.Position = new Vector2D(position.X, position.Y + (label.Height / 2f - icon.Height / 2f));
            label.Text = compo1Name + " + " + compo2Name + " = " + resultName;
        }

        public override void Render()
        {
            if (bgcol != Color.Transparent) Gorgon.Screen.FilledRectangle(clientArea.X, clientArea.Y, clientArea.Width, clientArea.Height, bgcol);
            icon.Draw();
            label.Draw();
        }

        public override void Dispose()
        {
            label = null;
            icon = null;
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

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (clientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
                bgcol = Color.SteelBlue;
            else
                bgcol = Color.Transparent;
            
        }
    }
}
