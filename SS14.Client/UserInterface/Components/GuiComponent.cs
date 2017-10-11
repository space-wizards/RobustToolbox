using System;
using System.Collections.Generic;
using Lidgren.Network;
using OpenTK.Graphics;
using SFML.Graphics;
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

        public int Width { get; set; }
        public int Height { get; set; }

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
        }

        public void RemoveComponent(GuiComponent child)
        {
            if(child == null)
                throw new InvalidOperationException();

            if(!_children.Contains(child))
                throw new InvalidOperationException();

            _children.Remove(child);
            child._parent = null;
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
        
        public GuiComponentType ComponentClass { get; protected set; }
        public virtual Vector2i Position { get; set; }

        public virtual Box2i ClientArea { get; set; }

        public int ZDepth { get; set; }

        public bool ReceiveInput { get; set; } = true;

        public virtual bool Focus { get; set; }

        public virtual void ComponentUpdate(params object[] args) { }

        public virtual void Update(float frameTime)
        {
            foreach (var child in _children)
            {
                child.Update(frameTime);
            }
        }

        public virtual void Render()
        {
            // todo: need ordered batching
            foreach (var child in _children)
            {
                child.Render();
            }
        }

        public virtual void Resize()
        {
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
