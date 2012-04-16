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
    public class TemplateGuiComponent : GuiComponent
    {
        public TemplateGuiComponent() : base ()
        {
            ComponentClass = GuiComponentType.Undefined;
        }

        public override void ComponentUpdate(params object[] args)
        {
        }

        public override void Update(float frameTime)
        {
        }

        public override void Render()
        {
        }

        public override void Resize()
        {
        }

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public override void HandleNetworkMessage(Lidgren.Network.NetIncomingMessage message)
        {
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                return true;
            }
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            return false;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            return false;
        }
    }
}
