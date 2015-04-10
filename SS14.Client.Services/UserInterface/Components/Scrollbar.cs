using Lidgren.Network;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Graphics.Sprite;
using System;
using System.Drawing;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Shared.Maths;

namespace SS14.Client.Services.UserInterface.Components
{
    public class Scrollbar : GuiComponent
    {
        //IMPORTANT: With this implementation you are not guaranteed to get a step size of 1. 
        //           If the Bar is shorter than the maximum value the step size will increase.
        //           If the Bar is longer than the maximum value the step size will decrease.
        //           The latter leads to actual 1 increment steps even though the step size will be below 1.
        //           Additionally the Min value is a fixed 0 right now.

        #region Delegates

        public delegate void ScrollbarChangedHandler(int newValue);

        #endregion

        private readonly TextSprite DEBUG;
        private readonly IResourceManager _resourceManager;
		private readonly CluwneSprite scrollbarButton;
        private bool DRAW_DEBUG = false;

        public bool Horizontal = false;
        private bool RaiseEvent;

        private int actualSize; //Actual max value of bar.

        private float actualVal; //The actual value of the current button position.
        private Rectangle clientAreaButton;
        private int currentPos; //The current button position in relation to location of scrollbar.
        private bool dragging; //Currently dragging the button?
        public bool drawBackground = true;
        public int max = 100; //Maximum value of the bar.

        public int size = 300; //Graphical length of the bar.

        public Scrollbar(bool horizontal, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;

            Horizontal = horizontal;
            if (Horizontal) scrollbarButton = _resourceManager.GetSprite("scrollbutton_h");
            else scrollbarButton = _resourceManager.GetSprite("scrollbutton_v");

            DEBUG = new TextSprite("DEBUGSLIDER", "Position:", _resourceManager.GetFont("CALIBRI"));
            DEBUG.Color = Color.OrangeRed;
            DEBUG.ShadowColor = Color.DarkBlue;
            DEBUG.Shadowed = true;
          //  DEBUG.ShadowOffset = new Vector2(1, 1);
            Update(0);
        }

        public float stepSize { private set; get; }

        public float Value
        {
            get { return actualVal; }
            set
            {
                actualVal = Math.Min(max, Math.Max(value, 0));
                currentPos = (int) Math.Max(Math.Round(actualVal/stepSize), 0);
                RaiseEvent = true;
            }
        }

        public event ScrollbarChangedHandler ValueChanged;

        public override void HandleNetworkMessage(NetIncomingMessage message)
        {
        }

		public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (!IsVisible()) return false;
            if (clientAreaButton.Contains((int) e.X, (int) e.Y))
            {
                dragging = true;
                return true;
            }
            else if (ClientArea.Contains((int) e.X, (int) e.Y))
            {
                return true;
            }
            return false;
        }

		public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (dragging)
            {
                dragging = false;
                return true;
            }
            return false;
        }

		public override void MouseMove(MouseMoveEventArgs e)
        {
            if (!IsVisible()) return;
            if (dragging)
            {
                if (Horizontal)
                    currentPos = (int) e.X - ClientArea.Location.X - (int) (scrollbarButton.Width/2f);
                else currentPos = (int) e.Y - ClientArea.Location.Y - (int) (scrollbarButton.Height/2f);
                currentPos = Math.Min(currentPos, actualSize);
                currentPos = Math.Max(currentPos, 0);
                RaiseEvent = true;
            }
        }

		public override bool MouseWheelMove(MouseWheelEventArgs e)
        {
            Value += ((Math.Sign(e.Y)*-1)*Math.Max(((max/20)), 1));
            return true;
        }

        public override void Update(float frameTime)
        {
            if (!IsVisible()) return;
            base.Update(frameTime);
            if (Horizontal)
            {
                ClientArea = new Rectangle(Position, new Size(size, (int) scrollbarButton.Height));
                clientAreaButton = new Rectangle(new Point(Position.X + currentPos, Position.Y),
                                                 new Size((int) scrollbarButton.Width, (int) scrollbarButton.Height));
                actualSize = size - (int) scrollbarButton.Width;
            }
            else
            {
                ClientArea = new Rectangle(Position, new Size((int) scrollbarButton.Width, size));
                clientAreaButton = new Rectangle(new Point(Position.X, Position.Y + currentPos),
                                                 new Size((int) scrollbarButton.Width, (int) scrollbarButton.Height));
                actualSize = size - (int) scrollbarButton.Height;
            }

            stepSize = (float) max/actualSize;
            actualVal = Math.Min((int) Math.Round(currentPos*stepSize), max);

            if (ValueChanged != null && RaiseEvent) //This is a bit ugly.
            {
                RaiseEvent = false;
                ValueChanged((int) actualVal);
            }
        }

        public override void Render()
        {
            if (!IsVisible()) return;
            if (drawBackground)
              CluwneLib.drawRectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, Color.DarkSlateGray);
            scrollbarButton.Draw(clientAreaButton);
            DEBUG.Position = new Vector2(ClientArea.Location.X + 20, ClientArea.Location.Y + 20);
            DEBUG.Text = "current: " + actualVal.ToString();
            if (DRAW_DEBUG) DEBUG.Draw();
           CluwneLib.drawRectangle(ClientArea.X + 0, ClientArea.Y + 0, ClientArea.Width - 0, ClientArea.Height - 0, Color.Black);
        }
    }
}