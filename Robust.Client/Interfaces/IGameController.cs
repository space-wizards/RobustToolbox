using Robust.Client.Input;

namespace Robust.Client.Interfaces
{
    public interface IGameController
    {
        void Shutdown(string reason=null);
    }

    internal interface IGameControllerInternal : IGameController
    {
        void KeyDown(KeyEventArgs keyEvent);
        void KeyUp(KeyEventArgs keyEvent);
        void TextEntered(TextEventArgs textEvent);
        void MouseDown(MouseButtonEventArgs mouseEvent);
        void MouseUp(MouseButtonEventArgs mouseButtonEventArgs);
        void MouseMove(MouseMoveEventArgs mouseMoveEventArgs);
        void MouseWheel(MouseWheelEventArgs mouseWheelEventArgs);
    }
}
