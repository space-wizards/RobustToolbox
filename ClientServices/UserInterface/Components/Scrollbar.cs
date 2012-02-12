using System;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Resource;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using Lidgren.Network;

namespace ClientServices.UserInterface.Components
{
    public class Scrollbar : GuiComponent
    {
        private readonly IResourceManager _resourceManager;

        //IMPORTANT: With this implementation you are not guaranteed to get a step size of 1. 
        //           If the Bar is shorter than the maximum value the step size will increase.
        //           If the Bar is longer than the maximum value the step size will decrease.
        //           The latter leads to actual 1 increment steps even though the step size will be below 1.
        //           Additionally the Min value is a fixed 0 right now.

        public delegate void ScrollbarChangedHandler(int newValue);
        public event ScrollbarChangedHandler ValueChanged;
        private bool RaiseEvent = false;

        private Rectangle clientAreaButton;

        private Sprite scrollbarButton;

        private TextSprite DEBUG;
        private bool DRAW_DEBUG = false;

        public bool drawBackground = true;

        public bool Horizontal = false;

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
                actualVal = Math.Min(max, Math.Max(value, 0));
                currentPos = (int)Math.Max(Math.Round(actualVal / stepSize),0);
                RaiseEvent = true;
            }
        }

        private bool dragging = false;   //Currently dragging the button?

        private int actualSize = 0;      //Actual max value of bar.

        private int currentPos = 0;      //The current button position in relation to location of scrollbar.
        private float actualVal = 0;     //The actual value of the current button position.

        public Scrollbar(bool horizontal,IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;

            Horizontal = horizontal;
            if (Horizontal) scrollbarButton = _resourceManager.GetSprite("scrollbutton_h");
            else scrollbarButton = _resourceManager.GetSprite("scrollbutton_v");

            DEBUG = new TextSprite("DEBUGSLIDER", "Position:", _resourceManager.GetFont("CALIBRI"));
            DEBUG.Color = Color.OrangeRed;
            DEBUG.ShadowColor = Color.DarkBlue;
            DEBUG.Shadowed = true;
            DEBUG.ShadowOffset = new Vector2D(1, 1);
            Update();
        }

        public override void HandleNetworkMessage(NetIncomingMessage message)
        {
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (!IsVisible()) return false;
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
            if (!IsVisible()) return;
            if (dragging)
            {
                if (Horizontal) currentPos = (int)e.Position.X - ClientArea.Location.X - (int)(scrollbarButton.Width / 2f);
                else currentPos = (int)e.Position.Y - ClientArea.Location.Y - (int)(scrollbarButton.Height / 2f);
                currentPos = Math.Min(currentPos, (int)actualSize);
                currentPos = Math.Max(currentPos, 0);
                RaiseEvent = true;
            }
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            Value += ((Math.Sign(e.WheelDelta) * -1) * Math.Max(((max / 20)), 1));
            return true;
        }

        public override void Update()
        {
            if (!IsVisible()) return;
            base.Update();
            if (Horizontal)
            {
                ClientArea = new Rectangle(Position, new Size(size, (int)scrollbarButton.Height));
                clientAreaButton = new Rectangle(new Point(Position.X + currentPos, Position.Y), new Size((int)scrollbarButton.Width, (int)scrollbarButton.Height));
                actualSize = size - (int)scrollbarButton.Width;
            }
            else
            {
                ClientArea = new Rectangle(Position, new Size((int)scrollbarButton.Width, size));
                clientAreaButton = new Rectangle(new Point(Position.X, Position.Y + currentPos), new Size((int)scrollbarButton.Width, (int)scrollbarButton.Height));
                actualSize = size - (int)scrollbarButton.Height;
            }

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
            if (!IsVisible()) return;
            Gorgon.Screen.BeginDrawing();
            if (drawBackground) Gorgon.Screen.FilledRectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, System.Drawing.Color.DarkSlateGray);
            scrollbarButton.Draw(clientAreaButton);
            DEBUG.Position = new Vector2D(ClientArea.Location.X + 20, ClientArea.Location.Y + 20);
            DEBUG.Text = "current: " + actualVal.ToString();
            if (DRAW_DEBUG) DEBUG.Draw();
            Gorgon.Screen.Rectangle(ClientArea.X + 0, ClientArea.Y + 0, ClientArea.Width - 0, ClientArea.Height - 0, System.Drawing.Color.Black);
            Gorgon.Screen.EndDrawing();
        }

    }
}
