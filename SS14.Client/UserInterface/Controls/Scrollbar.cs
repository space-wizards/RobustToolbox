using OpenTK.Graphics;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Resource;
using SS14.Client.ResourceManagement;
using SS14.Shared.Maths;
using System;
using SS14.Client.Graphics.Input;
using SS14.Client.Graphics.Sprites;
using SS14.Client.UserInterface.Controls;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.Components
{
    public class Scrollbar : Control
    {
        //IMPORTANT: With this implementation you are not guaranteed to get a step size of 1.
        //           If the Bar is shorter than the maximum value the step size will increase.
        //           If the Bar is longer than the maximum value the step size will decrease.
        //           The latter leads to actual 1 increment steps even though the step size will be below 1.
        //           Additionally the Min value is a fixed 0 right now.
        
        public delegate void ScrollbarChangedHandler(int newValue);
        
        private readonly TextSprite _debugText;
        private readonly Sprite _scrollbarButton;
        
        public bool Horizontal { get; }
        private bool _raiseEvent;

        private int _actualSize; //Actual max value of bar.

        private float _actualVal; //The actual value of the current button position.
        private Box2i _clientAreaButton;
        private int _currentPos; //The current button position in relation to location of scrollbar.
        private bool _dragging; //Currently dragging the button?
        public int Max = 100; //Maximum value of the bar.

        /// <summary>
        /// Multiplier added to the scroll delta, to increase scrolling speed.
        /// </summary>
        private const int Multipler = 10;

        /// <summary>
        /// Graphical total length of the bar in px.
        /// </summary>
        public int BarLength { get; set; } = 300;

        public Scrollbar(bool horizontal)
        {
            Horizontal = horizontal;
            if (Horizontal)
            {
                _scrollbarButton = _resourceCache.GetSprite("scrollbutton_h");
                BarLength = (int) _scrollbarButton.LocalBounds.Width;
            }
            else
            {
                _scrollbarButton = _resourceCache.GetSprite("scrollbutton_v");
                BarLength = (int)_scrollbarButton.LocalBounds.Height;
            }

            _debugText = new TextSprite("Position:", _resourceCache.GetResource<FontResource>(@"Fonts/CALIBRI.TTF").Font);
            _debugText.FillColor = new Color4(255, 128, 0, 255);
            _debugText.ShadowColor = new Color4(0, 0, 128, 255);
            _debugText.Shadowed = true;

            BackgroundColor = new Color4(47, 79, 79, 255);
            DrawBackground = true;

            Update(0);
        }

        public float StepSize { private set; get; }

        public float Value
        {
            get => _actualVal;
            set
            {
                _actualVal = Math.Max(0, Math.Min(value, Max));
                _currentPos = (int)Math.Max(Math.Round(_actualVal / StepSize), 0);
                _raiseEvent = true;
            }
        }

        public event ScrollbarChangedHandler ValueChanged;

        protected override void OnCalcRect() { }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (!Visible) return false;
            if (_clientAreaButton.Contains(e.X, e.Y))
            {
                _dragging = true;
                return true;
            }
            if (ClientArea.Contains(e.X, e.Y))
            {
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (_dragging)
            {
                _dragging = false;
                return true;
            }
            return false;
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            if (!Visible) return;
            if (!_dragging) return;

            if (Horizontal)
                _currentPos = e.X - Position.X - ClientArea.Left - (int) (_scrollbarButton.LocalBounds.Width / 2f);
            else
                _currentPos = e.Y - Position.Y - ClientArea.Top - (int) (_scrollbarButton.LocalBounds.Height / 2f);

            _currentPos = Math.Min(_currentPos, _actualSize);
            _currentPos = Math.Max(_currentPos, 0);
            _raiseEvent = true;
        }

        public override bool MouseWheelMove(MouseWheelScrollEventArgs e)
        {
            if (base.MouseWheelMove(e))
                return true;

            if (!Visible || Disposed)
                return false;
            
            Value += (e.Delta * -1) * Math.Max((Max / 20), 1) * Multipler;
            return true; // always consume scroll events
        }

        public override void Update(float frameTime)
        {
            if (!Visible) return;
            base.Update(frameTime);
            var bounds = _scrollbarButton.LocalBounds;
            if (Horizontal)
            {
                ClientArea = Box2i.FromDimensions(new Vector2i(), new Vector2i(BarLength, (int) bounds.Height));
                _clientAreaButton = Box2i.FromDimensions(Position.X + _currentPos, Position.Y,
                    (int) bounds.Width, (int) bounds.Height);
                _actualSize = BarLength - (int) bounds.Width;
            }
            else
            {
                ClientArea = Box2i.FromDimensions(new Vector2i(), new Vector2i((int) bounds.Width, BarLength));
                _clientAreaButton = Box2i.FromDimensions(Position.X, Position.Y + _currentPos,
                    (int) bounds.Width, (int) bounds.Height);
                _actualSize = BarLength - (int) bounds.Height;
            }

            StepSize = (float) Max / _actualSize;
            _actualVal = Math.Max(0, Math.Min((int) Math.Round(_currentPos * StepSize), Max));

            if (ValueChanged != null && _raiseEvent) //This is a bit ugly.
            {
                _raiseEvent = false;
                ValueChanged((int) _actualVal);
            }
        }

        /// <inheritdoc />
        protected override void DrawContents()
        {
            base.DrawContents();

            _scrollbarButton.SetTransformToRect(_clientAreaButton);
            _scrollbarButton.Draw();

            if (DebugEnabled)
            {
                _debugText.Text = "current: " + _actualVal;
                _debugText.Position = new Vector2i(_clientAreaButton.Left - 20, _clientAreaButton.Top);
                _debugText.Draw();
            }
        }
    }
}
