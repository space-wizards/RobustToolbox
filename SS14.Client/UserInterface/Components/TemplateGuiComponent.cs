using Lidgren.Network;
using SFML.Window;
using SS14.Shared;
using System;

namespace SS14.Client.UserInterface.Components
{
    public class TemplateGuiComponent : GuiComponent
    {
        public TemplateGuiComponent()
        {
            ComponentClass = GuiComponentType.Undefined;
        }

        public override void ComponentUpdate(params object[] args)
        {
            throw new NotSupportedException();
        }

        public override void Update(float frameTime)
        {
            throw new NotSupportedException();
        }

        public override void Render()
        {
            throw new NotSupportedException();
        }

        public override void Resize()
        {
            throw new NotSupportedException();
        }

        public override void Dispose()
        {
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override void HandleNetworkMessage(NetIncomingMessage message)
        {
            throw new NotSupportedException();
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(e.X, e.Y))
            {
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(e.X, e.Y))
            {
                return true;
            }
            return false;
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            throw new NotSupportedException();
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
