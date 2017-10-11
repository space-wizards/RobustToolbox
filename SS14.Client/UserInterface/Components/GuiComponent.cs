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

        public GuiComponent Parent
        {
            get => _parent;
            set
            {
                if(value == null)
                    throw new ArgumentNullException(nameof(value));

                _parent.RemoveComponent(this);
                _parent = value;
                _parent.AddComponent(this);
            }
        }

        public void AddComponent(GuiComponent child)
        {
            if(_children.Contains(child))
                throw new InvalidOperationException();

            _children.Add(child);
        }

        public void RemoveComponent(GuiComponent child)
        {
            if(!_children.Contains(child))
                throw new InvalidOperationException();

            _children.Remove(child);
        }

        public void Destroy()
        {
            foreach (var child in _children)
            {
                child.Destroy();
            }
        }
        
        public GuiComponentType ComponentClass { get; protected set; }
        public virtual Vector2i Position { get; set; }

        public virtual Box2i ClientArea
        {
            get;
            set;
        }

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

        public virtual void HandleNetworkMessage(NetIncomingMessage message) { }

        public virtual bool MouseDown(MouseButtonEventArgs e)
        {
            return false;
        }

        public virtual bool MouseUp(MouseButtonEventArgs e)
        {
            return false;
        }

        public virtual void MouseMove(MouseMoveEventArgs e) { }

        public virtual bool MouseWheelMove(MouseWheelEventArgs e)
        {
            return false;
        }

        public virtual bool KeyDown(KeyEventArgs e)
        {
            return false;
        }

        public virtual bool TextEntered(TextEventArgs e)
        {
            return false;
        }
    }
}
