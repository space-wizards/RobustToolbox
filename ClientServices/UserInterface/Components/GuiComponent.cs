using System;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Player;
using ClientInterfaces.UserInterface;
using ClientServices.Player;
using SS13_Shared;
using GorgonLibrary.InputDevices;

namespace ClientServices.UserInterface.Components
{
    public class GuiComponent : IGuiComponent
    {
        public GuiComponentType ComponentClass { get; protected set; }
        public Point Position { get; set; }
        public Rectangle ClientArea { get; set; }
        private bool _visible = true;
        private bool _recieveInput = true;

        public int ZDepth { get; set; }
        public bool RecieveInput { get { return _recieveInput; } set { _recieveInput = value; } }
        public bool Focus { get; set; }

        public object UserData;

        public virtual void ComponentUpdate(params object[] args)
        {
        }

        public virtual void Update()
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
            GC.SuppressFinalize(this);
        }

        public virtual void HandleNetworkMessage(Lidgren.Network.NetIncomingMessage message)
        {
        }

        public virtual bool MouseDown(MouseInputEventArgs e)
        {
            return false;
        }

        public virtual bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }

        public virtual void MouseMove(MouseInputEventArgs e)
        {
        }

        public virtual bool MouseWheelMove(MouseInputEventArgs e)
        {
            return false;
        }

        public virtual bool KeyDown(KeyboardInputEventArgs e)
        {
            return false;
        }
    }
}
