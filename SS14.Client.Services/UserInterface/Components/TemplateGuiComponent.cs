using Lidgren.Network;
using SS14.Shared;
using System;
using System.Drawing;
using SFML.Window;

namespace SS14.Client.Services.UserInterface.Components
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
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override void HandleNetworkMessage(NetIncomingMessage message)
        {
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.X, (int) e.Y)))
            {
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.X, (int) e.Y)))
            {
                return true;
            }
            return false;
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
        }

        public override bool MouseWheelMove(MouseWheelEventArgs e)
        {
            return false;
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            return false;
        }
    }
}