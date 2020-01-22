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
        void SetCommandLineArgs(CommandLineArgs args);
        bool LoadConfigAndUserData { get; set; }
        bool Startup();
        void MainLoop(GameController.DisplayMode mode);
        void KeyDown(KeyEventArgs keyEvent);
        void KeyUp(KeyEventArgs keyEvent);
        void TextEntered(TextEventArgs textEvent);
        void MouseMove(MouseMoveEventArgs mouseMoveEventArgs);
        void MouseWheel(MouseWheelEventArgs mouseWheelEventArgs);
        void OverrideMainLoop(IGameLoop gameLoop);
    }
}
