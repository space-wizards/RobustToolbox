using System.Net;
using Robust.Client.Input;
using Robust.Shared.Timing;

namespace Robust.Client.Interfaces
{
    public interface IGameController
    {
        InitialLaunchState LaunchState { get; }

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

    public sealed class InitialLaunchState
    {
        public bool FromLauncher { get; }
        public string ConnectAddress { get; }
        public string Ss14Address { get; }
        public DnsEndPoint ConnectEndpoint { get; }

        public InitialLaunchState(bool fromLauncher, string connectAddress, string ss14Address, DnsEndPoint connectEndpoint)
        {
            FromLauncher = fromLauncher;
            ConnectAddress = connectAddress;
            Ss14Address = ss14Address;
            ConnectEndpoint = connectEndpoint;
        }
    }
}
