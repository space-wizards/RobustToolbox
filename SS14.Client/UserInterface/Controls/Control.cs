using System;
using System.Collections.Generic;
using OpenTK.Graphics;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using Debug = System.Diagnostics.Debug;

namespace SS14.Client.UserInterface.Controls
{
    /// <summary>
    ///     Base GUI class that all other controls inherit from.
    /// </summary>
    public abstract class Control : IDisposable
    {
        private static readonly Color4 _dbgBoundColor = new Color4(255, 0, 0, 32);
        private static readonly Color4 _dbgFocusColor = new Color4(0, 0, 255, 32);

        public static bool GlobalDebug { get; set; } = false;

        protected static IResourceCache _resourceCache;
        protected static IUserInterfaceManager UiManager;

        private readonly List<Control> _children = new List<Control>();

        public object UserData;
        private Align _align;
        protected Box2i _clientArea;
        protected Box2i _localBounds;
        private Vector2i _localPos;
        private Box2i _margins;
        private Control _parent;
        protected Vector2i _screenPos;
        protected Vector2i _size;
        private Sprite _backgroundImage;

        /// <summary>
        ///     Image that is stretched to fit the background of the control. This is optional.
        /// </summary>
        public Sprite BackgroundImage
        {
            get => _backgroundImage;
            set
            {
                if (value != null)
                {
                    _backgroundImage?.Dispose();
                    _backgroundImage = new Sprite(value);
                }
                else
                {
                    _backgroundImage?.Dispose();
                    _backgroundImage = null;
                }
            }
        }

        /// <summary>
        ///     Color of the background of the control. This will be blended with the BackgroundImage if it
        ///     is set.
        /// </summary>
        public Color BackgroundColor { get; set; } = Color.White;

        /// <summary>
        ///     Color of the foreground of the control. This is usually the text.
        /// </summary>
        public virtual Color ForegroundColor { get; set; } = Color.Black;

        /// <summary>
        ///     Color of the border around the control.
        /// </summary>
        public Color BorderColor { get; set; } = Color.Black;

        /// <summary>
        ///     Width of the border lines of the control in px.
        /// </summary>
        public int BorderWidth { get; set; } = 1;

        /// <summary>
        ///     Should the border of the control be drawn?
        /// </summary>
        public bool DrawBorder { get; set; } = false;

        /// <summary>
        ///     Should the background image and/or color be drawn?
        /// </summary>
        public bool DrawBackground { get; set; } = false;

        /// <summary>
        ///     If a control is not visible, it is not drawn to screen, and does not accept input.
        /// </summary>
        public virtual bool Visible { get; set; } = true;

        /// <summary>
        ///     Total width of the control.
        /// </summary>
        public int Width
        {
            get => _size.X;
            set => _size = new Vector2i(value, _size.Y);
        }

        /// <summary>
        ///     Total height of the control.
        /// </summary>
        public int Height
        {
            get => _size.Y;
            set => _size = new Vector2i(_size.X, value);
        }

        public IReadOnlyList<Control> Children => _children;

        /// <summary>
        ///     The parent component that this one will be positioned relative to.
        ///     Setting this property is equivelent to calling AddControl() on the parent control.
        ///     TODO: Check for already having a parent
        /// </summary>
        public Control Parent
        {
            get => _parent;
            set => value.AddControl(this);
        }

        /// <summary>
        ///     Local position relative to parent control. This offsets the screen position
        ///     after the alignment calculations.
        /// </summary>
        public virtual Vector2i LocalPosition
        {
            get => _localPos;
            set => _localPos = value;
        }

        public virtual Box2i Margins
        {
            get => _margins;
            set => _margins = value;
        }

        /// <summary>
        ///     Total size of the control. This includes borders, title bars, scrollbars, etc.
        /// </summary>
        public Vector2i Size
        {
            get => _size;
            set => _size = value;
        }

        /// <summary>
        ///     How to align the control inside of its parent. Default is Top Left.
        /// </summary>
        public Align Alignment
        {
            get => _align;
            set => _align = value;
        }

        /// <summary>
        ///     Absolute screen position of control.
        /// </summary>
        public virtual Vector2i Position
        {
            get => _screenPos;
            set => _screenPos = value;
        }

        /// <summary>
        ///     the LOCAL area inside of the control for children.
        /// </summary>
        public virtual Box2i ClientArea
        {
            get => _clientArea;
            set => _clientArea = value;
        }

        /// <summary>
        ///     Draws debugging info over the control.
        /// </summary>
        public bool DebugEnabled { get; set; } = false;

        /// <summary>
        ///     Does the control accept input events? If false, children also don't get input.
        /// </summary>
        public bool ReceiveInput { get; set; } = true;

        [Obsolete("Controls are in a real tree structure now.")]
        public int ZDepth { get; set; }

        /// <summary>
        ///     True if this control is the one receiving keyboard input. Only one control can have focus
        ///     at a time in the GUI.
        /// </summary>
        public virtual bool Focus
        {
            get => UiManager.HasFocus(this);
            set
            {
                if (value)
                    UiManager.SetFocus(this);
                else
                    UiManager.RemoveFocus(this);
            }
        }

        /// <summary>
        ///     If this control is currently disposed.
        /// </summary>
        public bool Disposed { get; private set; } = false;

        protected Control()
        {
            _resourceCache = IoCManager.Resolve<IResourceCache>();
            UiManager = IoCManager.Resolve<IUserInterfaceManager>();
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            if (Disposed)
                return;

            Disposed = true;
            IoCManager.Resolve<IUserInterfaceManager>().RemoveComponent(this);

            DisposeAllChildren();
            Parent?.RemoveControl(this);

            // this disposes it
            BackgroundImage = null;

            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     called every frame, used to poll things.
        /// </summary>
        public virtual void Update(float frameTime)
        {
            foreach (var child in _children)
            {
                child.Update(frameTime);
            }
        }

        /// <summary>
        ///     Draws this control to the screen, as well as all child controls.
        /// </summary>
        public virtual void Draw()
        {
            if (!Visible) return;

            var rect = _clientArea.Translated(Position);

            if (DrawBackground)
                if (BackgroundImage != null)
                {
                    BackgroundImage.SetTransformToRect(_clientArea.Translated(_screenPos));
                    BackgroundImage.Color = BackgroundColor;
                    BackgroundImage.Draw();
                }
                else
                {
                    CluwneLib.drawRectangle(rect.Left, rect.Top, rect.Width, rect.Height, BackgroundColor);
                }

            if (DrawBorder)
                CluwneLib.drawHollowRectangle(rect.Left, rect.Top, rect.Width, rect.Height, BorderWidth, BorderColor);

            DrawContents();

            if (DebugEnabled || GlobalDebug)
            {
                CluwneLib.drawRectangle(rect.Left, rect.Top, rect.Width, rect.Height, Focus ? _dbgFocusColor : _dbgBoundColor);
            }

            // draw each of the children.
            // todo: need ordered batching
            foreach (var child in _children)
            {
                child.Draw();
            }
        }

        /// <summary>
        ///     Draws the contents of this control to the screen. This is called after the bg and border is drawn,
        ///     but before child Components are drawn. This is where you render text and such for this control.
        /// </summary>
        protected virtual void DrawContents() { }

        /// <summary>
        ///     Performs a full layout of this and all child controls. You want to call this after moving, adding, removing,
        ///     changing sizes, etc.
        ///     It does NOT have to be called every frame, just when things change.
        /// </summary>
        public virtual void DoLayout()
        {
            Layout?.Invoke(this, EventArgs.Empty);

            var rectArgs = new ClientRectEventArgs();
            CalcRect?.Invoke(this, rectArgs);
            if (!rectArgs.Consumed)
                OnCalcRect();

            var resizeArgs = new ResizeEventArgs();
            Resize?.Invoke(this, resizeArgs);
            if (!resizeArgs.Consumed)
                OnCalcPosition();

            foreach (var child in _children)
            {
                child.DoLayout();
            }
        }

        /// <summary>
        ///     A mouse button was pressed.
        /// </summary>
        public virtual bool MouseDown(MouseButtonEventArgs e)
        {
            var consumed = false;
            foreach (var child in _children)
            {
                if (!child.MouseDown(e))
                    continue;

                consumed = true;
                break;
            }

            return consumed;
        }

        /// <summary>
        ///     A mouse button was released.
        /// </summary>
        public virtual bool MouseUp(MouseButtonEventArgs e)
        {
            var consumed = false;
            foreach (var child in _children)
            {
                if (!child.MouseUp(e))
                    continue;

                consumed = true;
                break;
            }

            return consumed;
        }

        /// <summary>
        ///     The mouse cursor was moved.
        /// </summary>
        public virtual void MouseMove(MouseMoveEventArgs e)
        {
            foreach (var child in _children)
            {
                child.MouseMove(e);
            }
        }

        public virtual bool MouseWheelMove(MouseWheelScrollEventArgs e)
        {
            var consumed = false;
            foreach (var child in _children)
            {
                if (!child.MouseWheelMove(e))
                    continue;

                consumed = true;
                break;
            }

            return consumed;
        }

        public virtual bool KeyDown(KeyEventArgs e)
        {
            var consumed = false;
            foreach (var child in _children)
            {
                if (!child.KeyDown(e))
                    continue;

                consumed = true;
                break;
            }

            return consumed;
        }

        public virtual bool TextEntered(TextEventArgs e)
        {
            var consumed = false;
            foreach (var child in _children)
            {
                if (!child.TextEntered(e))
                    continue;

                consumed = true;
                break;
            }

            return consumed;
        }

        /// <summary>
        ///     The layout of this control is about to be recalculated. This is called before any other layout
        ///     functions/events. This is a good place to set custom Sizes of controls.
        /// </summary>
        public event EventHandler<EventArgs> Layout;

        /// <summary>
        ///     The client rectangle is about to be calculated. This is called after the Layout event, and before
        ///     OnLayout. Another good place to set Sizes of things if you need to.
        /// </summary>
        public event EventHandler<ClientRectEventArgs> CalcRect;

        /// <summary>
        ///     The absolute screen coords (Position property) of this control are about to be calculated. This is a good
        ///     place to set up relative positioning with LocalPosition and Alignment. ClientArea is valid at this point,
        ///     Along with Size.
        /// </summary>
        public event EventHandler<ResizeEventArgs> Resize;

        /// <summary>
        ///     Called right after the CalcRect event, this is where you calculate the client rectangle bounds of this
        ///     control. Remember that the bounds are in Local coordinates, so don't use absolute screen coordinates.
        ///     You will prob need the Size of this control, so make sure that is already set up properly.
        /// </summary>
        protected abstract void OnCalcRect();

        /// <summary>
        ///     Recalculates the absolute position of the control on the screen. This uses the Parent, LocalPosition, ClientArea,
        ///     and Alignment variables.
        /// </summary>
        protected virtual void OnCalcPosition()
        {
            if (Parent == null)
            {
                _screenPos = _localPos;
            }
            else
            {
                var parentPos = Parent.Position;
                var parentRect = Parent.ClientArea;

                // horizontal
                int scrX;
                if ((Alignment & Align.HCenter) == Align.HCenter)
                    scrX = parentRect.Width / 2 - Width / 2;
                else if ((Alignment & Align.Right) == Align.Right)
                    scrX = parentRect.Width + _localPos.X;
                else // aligning left = not aligning
                    scrX = _localPos.X;

                // vertical
                int scrY;
                if ((Alignment & Align.VCenter) == Align.VCenter)
                    scrY = parentRect.Height / 2 - Height / 2;
                else if ((Alignment & Align.Bottom) == Align.Bottom)
                    scrY = parentRect.Height + _localPos.Y;
                else // aligning top == not aligning
                    scrY = _localPos.Y;

                _screenPos = new Vector2i(scrX, scrY) + parentPos;
            }
        }

        /// <summary>
        ///     Applies style to this control and all children.
        /// </summary>
        public virtual void ApplyStyle(Style style)
        {
            //Color myself

            foreach (var child in Children)
            {
                child.ApplyStyle(style);
            }
        }

        /// <summary>
        ///     Adds a child control to this control.
        /// </summary>
        public void AddControl(Control child)
        {
            if (_children.Contains(child))
                throw new InvalidOperationException();

            if (this == child)
                throw new InvalidOperationException();

            child._parent = this;
            _children.Add(child);
        }

        /// <summary>
        ///     Removes a child control from this control.
        /// </summary>
        public void RemoveControl(Control child)
        {
            if (child == null)
                throw new InvalidOperationException();

            if (!_children.Contains(child))
                throw new InvalidOperationException();

            _children.Remove(child);
            child._parent = null;
        }

        /// <summary>
        ///     Removes all child controls from this control.
        /// </summary>
        public void DisposeAllChildren()
        {
            var children = new List<Control>(_children); // must cache list because children modify it
            foreach (var child in children)
            {
                child.Dispose();
            }
        }

        /// <summary>
        ///     Calculates a box that contains all of the children of this control.
        /// </summary>
        internal Box2i GetShrinkBounds(bool includeMe = true)
        {
            var bounds = new Box2i();

            foreach (var control in Children)
            {
                bounds = Union(bounds, control.GetShrinkBounds());
            }

            return includeMe ? Union(bounds, ClientArea.Translated(Position)) : bounds;
        }

        /// <summary>
        ///     Calculates the union of two AABB's.
        /// </summary>
        private static Box2i Union(Box2i a, Box2i b)
        {
            Debug.Assert(a.Top <= a.Bottom);
            Debug.Assert(a.Left <= a.Right);

            Debug.Assert(b.Top <= b.Bottom);
            Debug.Assert(b.Left <= b.Right);

            var left = Math.Min(a.Left, b.Left);
            var right = Math.Max(a.Right, b.Right);

            var top = Math.Min(a.Top, b.Top);
            var bottom = Math.Max(a.Bottom, b.Bottom);

            return new Box2i(left, top, right, bottom);
        }

        public class ResizeEventArgs : EventArgs
        {
            public bool Consumed { get; set; } = false;
        }

        public class ClientRectEventArgs : EventArgs
        {
            public bool Consumed { get; set; } = false;
        }
    }
}
