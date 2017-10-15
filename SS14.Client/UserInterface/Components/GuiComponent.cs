using Lidgren.Network;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.Components
{
    public class GuiComponent : IGuiComponent
    {
        public object UserData;
        private bool _receiveInput = true;
        private bool _visible = true;

        public string name { get; protected set; }

        #region IGuiComponent Members

        public GuiComponentType ComponentClass { get; protected set; }
        public virtual Vector2i Position { get; set; }
        public virtual Box2i ClientArea { get; set; }

        public int ZDepth { get; set; }

        public bool ReceiveInput
        {
            get { return _receiveInput; }
            set { _receiveInput = value; }
        }

        public virtual bool Focus { get; set; }

        public virtual void ComponentUpdate(params object[] args)
        {
            //TODO: Figure out what to do with this
        }

        public virtual void Update(float frameTime)
        {
            //TODO: Figure out what to do with this
        }

        public virtual void Render()
        {
            //TODO: Figure out what to do with this
        }

        public virtual void Resize()
        {
            //TODO: Figure out what to do with this
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

        public virtual void HandleNetworkMessage(NetIncomingMessage message)
        {
            //TODO: Figure out what to do with this
        }

        public virtual bool MouseDown(MouseButtonEventArgs e)
        {
            return false;
        }

        public virtual bool MouseUp(MouseButtonEventArgs e)
        {
            return false;
        }

        public virtual void MouseMove(MouseMoveEventArgs e)
        {
            //TODO: Figure out what to do with this
        }

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

        #endregion IGuiComponent Members
    }
}
