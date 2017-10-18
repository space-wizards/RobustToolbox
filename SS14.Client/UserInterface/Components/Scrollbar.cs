using Lidgren.Network;
using OpenTK.Graphics;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Interfaces.Resource;
using SS14.Client.ResourceManagement;
using SS14.Shared.Maths;
using System;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.Components
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

        #endregion Delegates

        private readonly TextSprite DEBUG;
        private readonly IResourceCache _resourceCache;
        private readonly Sprite scrollbarButton;
        private bool DRAW_DEBUG = false;

        public bool Horizontal = false;
        private bool RaiseEvent;

        private int actualSize; //Actual max value of bar.

        private float actualVal; //The actual value of the current button position.
        private Box2i clientAreaButton;
        private int currentPos; //The current button position in relation to location of scrollbar.
        private bool dragging; //Currently dragging the button?
        public bool drawBackground = true;
        public int max = 100; //Maximum value of the bar.

        /// <summary>
        /// Multiplier added to the scroll delta, to increase scrolling speed.
        /// </summary>
        public int multipler = 10;

        public int size = 300; //Graphical length of the bar.

        public Scrollbar(bool horizontal, IResourceCache resourceCache)
        {
            _resourceCache = resourceCache;

            Horizontal = horizontal;
            if (Horizontal) scrollbarButton = _resourceCache.GetSprite("scrollbutton_h");
            else scrollbarButton = _resourceCache.GetSprite("scrollbutton_v");

            DEBUG = new TextSprite("DEBUGSLIDER", "Position:", _resourceCache.GetResource<FontResource>(@"Fonts/CALIBRI.TTF").Font);
            DEBUG.Color = new Color4(255, 128, 0, 255);
            DEBUG.ShadowColor = new Color4(0, 0, 128, 255);
            DEBUG.Shadowed = true;
            Update(0);
        }

        public float stepSize { private set; get; }

        public float Value
        {
            get { return actualVal; }
            set
            {
                actualVal = Math.Max(0, Math.Min(value, max));
                currentPos = (int)Math.Max(Math.Round(actualVal / stepSize), 0);
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
            if (clientAreaButton.Contains((int)e.X, (int)e.Y))
            {
                dragging = true;
                return true;
            }
            else if (ClientArea.Contains((int)e.X, (int)e.Y))
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
                    currentPos = (int)e.X - ClientArea.Left - (int)(scrollbarButton.GetLocalBounds().Width / 2f);
                else currentPos = (int)e.Y - ClientArea.Top - (int)(scrollbarButton.GetLocalBounds().Height / 2f);
                currentPos = Math.Min(currentPos, actualSize);
                currentPos = Math.Max(currentPos, 0);
                RaiseEvent = true;
            }
        }

        public override bool MouseWheelMove(MouseWheelEventArgs e)
        {
            Value += (e.Delta * -1) * Math.Max((max / 20), 1) * multipler;
            return true;
        }

        public override void Update(float frameTime)
        {
            if (!IsVisible()) return;
            base.Update(frameTime);
            var bounds = scrollbarButton.GetLocalBounds();
            if (Horizontal)
            {
                ClientArea = Box2i.FromDimensions(Position, new Vector2i(size, (int)bounds.Height));
                clientAreaButton = Box2i.FromDimensions(Position.X + currentPos, Position.Y,
                                                 (int)bounds.Width, (int)bounds.Height);
                actualSize = size - (int)bounds.Width;
            }
            else
            {
                ClientArea = Box2i.FromDimensions(Position, new Vector2i((int)bounds.Width, size));
                clientAreaButton = Box2i.FromDimensions(Position.X, Position.Y + currentPos,
                                                 (int)bounds.Width, (int)bounds.Height);
                actualSize = size - (int)bounds.Height;
            }

            stepSize = (float)max / actualSize;
            actualVal = Math.Max(0, Math.Min((int)Math.Round(currentPos * stepSize), max));

            if (ValueChanged != null && RaiseEvent) //This is a bit ugly.
            {
                RaiseEvent = false;
                ValueChanged((int)actualVal);
            }
        }

        public override void Render()
        {
            if (!IsVisible()) return;
            if (drawBackground)
                CluwneLib.drawRectangle(ClientArea.Left, ClientArea.Top, ClientArea.Width, ClientArea.Height, new Color4(47, 79, 79, 255));
            scrollbarButton.SetTransformToRect(clientAreaButton);
            scrollbarButton.Draw();
            DEBUG.Position = new Vector2i(ClientArea.Left + 20, ClientArea.Top + 20);
            DEBUG.Text = "current: " + actualVal.ToString();
            if (DRAW_DEBUG) DEBUG.Draw();
        }
    }
}
