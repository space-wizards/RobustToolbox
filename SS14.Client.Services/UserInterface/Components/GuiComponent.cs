using SFML.Window;
using Lidgren.Network;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared;
using SS14.Shared.IoC;
using System;
using System.Drawing;

namespace SS14.Client.Services.UserInterface.Components
{
    public class GuiComponent : IGuiComponent
    {
        public object UserData;
        private bool _recieveInput = true;
        private bool _visible = true;

        #region IGuiComponent Members

        public GuiComponentType ComponentClass { get; protected set; }
        public virtual Point Position { get; set; }
        public virtual Rectangle ClientArea { get; set; }

        public int ZDepth { get; set; }

        public bool RecieveInput
        {
            get { return _recieveInput; }
            set { _recieveInput = value; }
        }

        public bool Focus { get; set; }

        public virtual void ComponentUpdate(params object[] args)
        {
        }

        public virtual void Update(float frameTime)
        {
        }

        public virtual void Render()
        {
        }

        public virtual void Resize()
        {
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
        }

		public virtual bool MouseWheelMove(MouseWheelEventArgs e)
        {
            return false;
        }

		public virtual bool KeyDown(KeyEventArgs e)
        {
            return false;
        }

        #endregion
    }
}