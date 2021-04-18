using System;
using Robust.Client.Input;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Robust.Client
{
    internal interface IGameControllerInternal : IGameController
    {
        GameControllerOptions Options { get; }
        void SetCommandLineArgs(CommandLineArgs args);
        bool LoadConfigAndUserData { get; set; }
        bool Startup(Func<ILogHandler>? logHandlerFactory = null);
        void MainLoop(GameController.DisplayMode mode);
        void KeyDown(KeyEventArgs keyEvent);
        void KeyUp(KeyEventArgs keyEvent);
        void TextEntered(TextEventArgs textEvent);
        void MouseMove(MouseMoveEventArgs mouseMoveEventArgs);
        void MouseWheel(MouseWheelEventArgs mouseWheelEventArgs);
        void OverrideMainLoop(IGameLoop gameLoop);
    }
}
