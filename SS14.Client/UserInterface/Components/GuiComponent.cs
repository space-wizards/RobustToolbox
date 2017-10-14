using System;
using System.Collections.Generic;
using Lidgren.Network;
using SFML.Window;
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

        public int Width
        {
            get => _size.X;
            set
            {
                _size = new Vector2i(value, _size.Y);
                Resize();
            }
        }

        public int Height
        {
            get => _size.Y;
            set
            {
                _size = new Vector2i(_size.X, value);
                Resize();
            }
        }

        public IReadOnlyList<GuiComponent> Children => _children;
        public GuiComponent Parent => _parent;

        /// <summary>
        ///     Local position relative to parent control (modified by Alignment).
        /// </summary>
        public virtual Vector2i LocalPosition
        {
            get => _localPos;
            set
            {
                _localPos = value;
                Resize();
            }
        }

        public virtual Box2i Margins
        {
            get => _margins;
            set
            {
                _margins = value;
                Resize();
            }
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
            set
            {
                _align = value;
                Resize();
            }
        }

        /// <summary>
        ///     Absolute screen position of control.
        /// </summary>
        public virtual Vector2i Position
        {
            get => _screenPos;
            set
            {
                _screenPos = value;
                Resize();
            }
        }

        /// <summary>
        ///     the LOCAL area inside of the control for children.
        /// </summary>
        public virtual Box2i ClientArea
        {
            get => _clientArea;
            set
            {
                _clientArea = value;
                Resize();
            }
        }

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
        }

        /// <summary>
        ///     Moves and/or changes size of the control.
        /// </summary>
        public virtual void Resize()
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
                if ((Alignment & Align.HCenter) != 0)
                    scrX = parentRect.Width / 2 - Width / 2;

                else if ((Alignment & Align.Right) != 0)
                    scrX = parentRect.Width + _localPos.X;

                else // aligning left = not aligning
                    scrX = _localPos.X;

                // vertical
                if((Alignment & Align.VCenter) != 0)
                    scrY = parentRect.Height / 2 - Height / 2;

                else if ((Alignment & Align.Bottom) != 0)
                    scrY = parentRect.Height + _localPos.Y;

                else // aligning top == not aligning
                    scrY = _localPos.Y;
                
                _screenPos = new Vector2i(scrX, scrY) + parentPos;
            }


            foreach (var child in _children)
            {
                child.Resize();
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
        /// Cleans up any resources.
        /// </summary>
        public virtual void Dispose()
        {
            var _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();
            _userInterfaceManager.RemoveComponent(this);
            GC.SuppressFinalize(this);
        }

        [Obsolete("Really, UI accepting raw packets? Really?")]
        public virtual void HandleNetworkMessage(NetIncomingMessage message) { }

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

        public void AddComponent(GuiComponent child)
        {
            if (_children.Contains(child))
                throw new InvalidOperationException();

            if (this == child)
                throw new InvalidOperationException();

            child._parent = this;
            _children.Add(child);
            child.Resize();
        }

        public void RemoveComponent(GuiComponent child)
        {
            if (child == null)
                throw new InvalidOperationException();

            if (!_children.Contains(child))
                throw new InvalidOperationException();

            _children.Remove(child);
            child._parent = null;
            child.Resize();
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
    }
}
