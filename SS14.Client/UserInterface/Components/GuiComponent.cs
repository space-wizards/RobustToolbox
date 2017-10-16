using System;
using System.Collections.Generic;
using Lidgren.Network;
using OpenTK.Graphics;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Components
{
    public abstract class GuiComponent : IGuiComponent
    {
        protected readonly List<GuiComponent> _children = new List<GuiComponent>();
        protected Align _align;
        protected Anchor _anchors;
        protected Box2i _clientArea;
        protected Box2i _localBounds;
        protected Vector2i _localPos;
        protected Box2i _margins;
        protected GuiComponent _parent;
        protected Vector2i _screenPos;

        protected Vector2i _size;
        private bool _visible = true;
        protected bool PctMgn = false;
        protected bool PctPos = false;
        public object UserData;

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

        public IReadOnlyList<GuiComponent> Children => _children;
        public GuiComponent Parent => _parent;

        /// <summary>
        ///     Local position relative to parent control (modified by Alignment).
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
        public Vector2i Size => _size;

        /// <summary>
        ///     How to align the control inside of its parent. Default is Top Left.
        /// </summary>
        public virtual Align Alignment
        {
            get => _align;
            set => _align = value;
        }

        public Anchor Anchors
        {
            get => _anchors;
            set => _anchors = value;
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


        public bool Debug { get; set; } = true;
        public GuiComponentType ComponentClass { get; protected set; }
        public int ZDepth { get; set; }
        public bool ReceiveInput { get; set; } = true;
        public virtual bool Focus { get; set; }
        public virtual void ComponentUpdate(params object[] args) { }

        /// <summary>
        ///     called regularly
        /// </summary>
        public virtual void Update(float frameTime)
        {
            foreach (var child in _children)
            {
                child.Update(frameTime);
            }
        }

        /// <summary>
        ///     draws the control to the screen.
        /// </summary>
        public virtual void Render()
        {
            // todo: need ordered batching
            foreach (var child in _children)
            {
                child.Render();
            }

            if(Debug)
            {
                var rect = _clientArea.Translated(Position);
                CluwneLib.drawRectangle(rect.Left, rect.Top, rect.Width, rect.Height, new Color4(255, 0, 0, 32));
            }
        }

        public void DoLayout()
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

        public virtual void ToggleVisible()
        {
            _visible = !_visible;
        }

        public virtual void SetVisible(bool vis)
        {
            _visible = vis;
        }

        public bool IsVisible()
        {
            return _visible;
        }

        /// <summary>
        ///     Cleans up any resources.
        /// </summary>
        public virtual void Dispose()
        {
            var _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();
            _userInterfaceManager.RemoveComponent(this);
            GC.SuppressFinalize(this);
        }

        [Obsolete("Really, UI accepting raw packets? Really?")]
        public virtual void HandleNetworkMessage(NetIncomingMessage message) { }

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

        public virtual bool MouseWheelMove(MouseWheelEventArgs e)
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
        ///     The layout of this control is about to be recalculated.
        /// </summary>
        public event EventHandler<EventArgs> Layout;

        /// <summary>
        ///     The client rectangle is about to be calculated.
        /// </summary>
        public event EventHandler<ClientRectEventArgs> CalcRect;

        /// <summary>
        ///     The form is about to be repositioned on screen.
        /// </summary>
        public event EventHandler<ResizeEventArgs> Resize;

        /// <summary>
        ///     Calculates the client rectangle inside of the control.
        /// </summary>
        protected abstract void OnCalcRect();

        /// <summary>
        ///     Recalculates the position of the control on the screen.
        /// </summary>
        protected virtual void OnCalcPosition()
        {
            int scrX;
            int scrY;

            if (Parent == null)
            {
                _screenPos = _localPos;
            }
            else
            {
                var parentPos = Parent.Position;
                var parentRect = Parent.ClientArea;

                // horizontal
                if ((Alignment & Align.HCenter) == Align.HCenter)
                    scrX = parentRect.Width / 2 - Width / 2;

                else if ((Alignment & Align.Right) == Align.Right)
                    scrX = parentRect.Width + _localPos.X;

                else // aligning left = not aligning
                    scrX = _localPos.X;

                // vertical
                if ((Alignment & Align.VCenter) == Align.VCenter)
                    scrY = parentRect.Height / 2 - Height / 2;

                else if ((Alignment & Align.Bottom) == Align.Bottom)
                    scrY = parentRect.Height + _localPos.Y;

                else // aligning top == not aligning
                    scrY = _localPos.Y;

                _screenPos = new Vector2i(scrX, scrY) + parentPos;
            }
        }

        public void AddComponent(GuiComponent child)
        {
            if (_children.Contains(child))
                throw new InvalidOperationException();

            if (this == child)
                throw new InvalidOperationException();

            child._parent = this;
            _children.Add(child);
        }

        public void RemoveComponent(GuiComponent child)
        {
            if (child == null)
                throw new InvalidOperationException();

            if (!_children.Contains(child))
                throw new InvalidOperationException();

            _children.Remove(child);
            child._parent = null;
            child.OnCalcPosition();
        }

        public void Destroy()
        {
            var children = new List<GuiComponent>(_children);
            foreach (var child in children)
            {
                child.Destroy();
            }

            Parent?.RemoveComponent(this);
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
