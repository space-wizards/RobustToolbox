using System;
using System.Drawing;
using GorgonLibrary.InputDevices;
using Lidgren.Network;
using SS13_Shared;

namespace ClientServices.UserInterface.Components
{
    public class TemplateGuiComponent : GuiComponent
    {
        public TemplateGuiComponent()
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

        public override void HandleNetworkMessage(NetIncomingMessage message)
        {
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
            {
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
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