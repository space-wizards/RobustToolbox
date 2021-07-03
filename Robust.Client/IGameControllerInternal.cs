using System;
using Robust.Client.Input;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Robust.Client
{
    internal interface IGameControllerInternal : IGameController
    {
        GameControllerOptions Options { get; }
        bool ContentStart { get; set; }
        void SetCommandLineArgs(CommandLineArgs args);
        void Run(GameController.DisplayMode mode, GameControllerOptions options, Func<ILogHandler>? logHandlerFactory = null);
        void KeyDown(KeyEventArgs keyEvent);
        void KeyUp(KeyEventArgs keyEvent);
        void TextEntered(TextEventArgs textEvent);
        void MouseMove(MouseMoveEventArgs mouseMoveEventArgs);
        void MouseWheel(MouseWheelEventArgs mouseWheelEventArgs);
        void OverrideMainLoop(IGameLoop gameLoop);
    }
}
