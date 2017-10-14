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
        protected GuiComponent _parent;
        private bool _visible = true;
        public object UserData;

        protected Vector2i _size;
        protected Box2i _margins;
        protected Vector2i _localPos;
        protected Vector2i _screenPos;
        protected Anchor _anchors;
        protected Box2i _clientArea;
        protected bool PctPos = false;
        protected bool PctMgn = false;

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

        public void AddComponent(GuiComponent child)
        {
            if(_children.Contains(child))
                throw new InvalidOperationException();

            if(this == child)
                throw new InvalidOperationException();

            child._parent = this;
            _children.Add(child);
            child.Resize();
        }

        public void RemoveComponent(GuiComponent child)
        {
            if(child == null)
                throw new InvalidOperationException();

            if(!_children.Contains(child))
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
        
        public virtual Vector2i Position
        {
            get => _screenPos;
            set
            {
                _screenPos = value;
                Resize();
            }
        }

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

        public virtual Anchor Anchors
        {
            get => _anchors;
            set
            {
                _anchors = value;
                Resize();
            }
        }

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
        /// called regularly
        /// </summary>
        public virtual void Update(float frameTime)
        {
            foreach (var child in _children)
            {
                child.Update(frameTime);
            }
        }

        /// <summary>
        /// draws the control to the screen.
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
        /// Moves and/or changes size of the control.
        /// </summary>
        public virtual void Resize()
        {
            Vector2i pos = new Vector2i();

            if (Parent == null)
                pos += _localPos;
            else
            {
                var parentRect = Parent.ClientArea;
                if (Anchors == Anchor.None)
                    pos += _localPos;
                else if ((Anchors & Anchor.HCenter) != 0)
                {
                    pos += new Vector2i((parentRect.Width / 2) - (Width / 2), _localPos.Y);
                }
                else if((Anchors & Anchor.Right) != 0)
                {
                    pos += (new Vector2i(parentRect.Width, 0) + _localPos);
                }
            }
            
            _screenPos = pos;

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
    }
}
