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
    public class ScrollbarHorizontal : GuiComponent
    {
        public override Point Position
        {
            get
            {
                return base.Position;
            }
            set
            {
                base.Position = value;
            }
        }

        //IMPORTANT: With this implementation you are not guaranteed to get a step size of 1. 
        //           If the Bar is shorter than the maximum value the step size will increase.
        //           If the Bar is longer than the maximum value the step size will decrease.
        //           The latter leads to actual 1 increment steps even though the step size will be below 1.
        //           Additionally the Min value is a fixed 0 right now.

        public delegate void ValueChangedHandler(int newValue);
        public event ValueChangedHandler ValueChanged;
        private bool RaiseEvent = false;

        private Rectangle clientArea;
        private Rectangle clientAreaButton;

        private GUIElement scrollbarButton;

        private TextSprite DEBUG;
        private bool DRAW_DEBUG = false;

        public bool drawBackground = true;

        public float stepSize { private set; get; } //How much one "step" on the bar counts as towards the actual current value.

        public int max = 100;            //Maximum value of the bar.

        public int size = 300;           //Graphical length of the bar.

        public float Value
        {
            get
            {
                return actualVal;
            }
            set
            {
                actualVal = value;
                currentPos = (int)Math.Round(value / stepSize);
                RaiseEvent = true;
            }
        }

        private bool dragging = false;   //Currently dragging the button?

        private int actualSize = 0;      //Actual max value of bar.

        private int currentPos = 0;      //The current button position in relation to location of scrollbar.
        private float actualVal = 0;     //The actual value of the current button position.

        public ScrollbarHorizontal()
            : base()
        {
            scrollbarButton = UiManager.Singleton.Skin.Elements["Controls.Scrollbar.Button.Vertical"];
            DEBUG = new TextSprite("DEBUGSLIDER","Position:", ResMgr.Singleton.GetFont("CALIBRI"));
            DEBUG.Color = System.Drawing.Color.OrangeRed;
            DEBUG.ShadowColor = System.Drawing.Color.DarkBlue;
            DEBUG.Shadowed = true;
            DEBUG.ShadowOffset = new Vector2D(1, 1);
        }

        public override void HandleNetworkMessage(NetIncomingMessage message)
        {
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (clientAreaButton.Contains((int)e.Position.X, (int)e.Position.Y))
            {
                dragging = true;
                return true;
            }
            return false;
        }

        
        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (dragging)
            {
                dragging = false;
                return true;
            }
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (dragging)
            {
                currentPos = (int)e.Position.X - clientArea.Location.X - (int)(scrollbarButton.Dimensions.Width / 2f);
                currentPos = Math.Min(currentPos, (int)actualSize);
                currentPos = Math.Max(currentPos, 0);
                RaiseEvent = true;
            }
        }

        public override void Update()
        {
            base.Update();
            clientArea = new Rectangle(position, new Size(size,scrollbarButton.Dimensions.Height));
            clientAreaButton = new Rectangle(new Point(position.X + currentPos, position.Y), new Size(scrollbarButton.Dimensions.Width, scrollbarButton.Dimensions.Height));
            actualSize = size - scrollbarButton.Dimensions.Width;
            stepSize = (float)max / actualSize;
            actualVal = Math.Min((int)Math.Round(currentPos * stepSize), max);

            if (ValueChanged != null && RaiseEvent) //This is a bit ugly.
            {
                RaiseEvent = false;
                ValueChanged((int)actualVal);
            }
        }

        public override void Render()
        {
            Gorgon.Screen.BeginDrawing();
            if (drawBackground) Gorgon.Screen.FilledRectangle(clientArea.X, clientArea.Y, clientArea.Width, clientArea.Height, System.Drawing.Color.DarkSlateGray);
            scrollbarButton.Draw(clientAreaButton);
            DEBUG.Position = new Vector2D(clientArea.Location.X, clientArea.Location.Y+20);
            DEBUG.Text = "current: " + actualVal.ToString();
            if(DRAW_DEBUG) DEBUG.Draw();
            Gorgon.Screen.EndDrawing();
        }

    }
}
