using Robust.Client.Input;
using Robust.Shared.Timing;

namespace Robust.Client.Interfaces
{
    public interface IGameController
    {
        void Shutdown(string reason=null);
    }

    internal interface IGameControllerInternal : IGameController
    {
        bool LoadConfigAndUserData { get; set; }
        void Startup();
        void MainLoop(GameController.DisplayMode mode);
        string ContentRootDir { get; set; }
        void KeyDown(KeyEventArgs keyEvent);
        void KeyUp(KeyEventArgs keyEvent);
        void TextEntered(TextEventArgs textEvent);
        void MouseDown(MouseButtonEventArgs mouseEvent);
        void MouseUp(MouseButtonEventArgs mouseButtonEventArgs);
        void MouseMove(MouseMoveEventArgs mouseMoveEventArgs);
        void MouseWheel(MouseWheelEventArgs mouseWheelEventArgs);
        void OverrideMainLoop(IGameLoop gameLoop);
    }
}
