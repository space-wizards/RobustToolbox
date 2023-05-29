using System;
using Robust.Client;
using Robust.Client.Input;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Robust.UnitTesting
{
    internal sealed class GameControllerDummy : IGameControllerInternal
    {
        public InitialLaunchState LaunchState { get; } = new(false, null, null, null);
        public GameControllerOptions Options { get; } = new();
        public bool ContentStart { get; set; }

        public event Action<FrameEventArgs>? TickUpdateOverride;

        public void Shutdown(string? reason = null)
        {
        }

        public void Redial(string address, string? reason = null)
        {
        }

        public void SetCommandLineArgs(CommandLineArgs args)
        {
        }

        public bool LoadConfigAndUserData { get; set; } = true;

        public bool Startup(Func<ILogHandler>? logHandlerFactory = null)
        {
            return true;
        }

        public void MainLoop(GameController.DisplayMode mode)
        {
        }

        public string? ContentRootDir { get; set; }

        public void Run(GameController.DisplayMode mode, GameControllerOptions options, Func<ILogHandler>? logHandlerFactory = null)
        {
        }

        public void KeyDown(KeyEventArgs keyEvent)
        {
        }

        public void KeyUp(KeyEventArgs keyEvent)
        {
        }

        public void TextEntered(TextEnteredEventArgs textEnteredEvent)
        {
        }

        public void MouseMove(MouseMoveEventArgs mouseMoveEventArgs)
        {
        }

        public void MouseWheel(MouseWheelEventArgs mouseWheelEventArgs)
        {
        }

        public void OverrideMainLoop(IGameLoop gameLoop)
        {
        }
    }
}
