using Robust.Client.Input;
using Robust.Client.Interfaces;

namespace Robust.UnitTesting
{
    internal sealed class GameControllerDummy : IGameControllerInternal
    {
        public void Shutdown(string reason = null)
        {
        }

        public void KeyDown(KeyEventArgs keyEvent)
        {
        }

        public void KeyUp(KeyEventArgs keyEvent)
        {
        }

        public void TextEntered(TextEventArgs textEvent)
        {
        }

        public void MouseDown(MouseButtonEventArgs mouseEvent)
        {
        }

        public void MouseUp(MouseButtonEventArgs mouseButtonEventArgs)
        {
        }

        public void MouseMove(MouseMoveEventArgs mouseMoveEventArgs)
        {
        }

        public void MouseWheel(MouseWheelEventArgs mouseWheelEventArgs)
        {
        }
    }
}
