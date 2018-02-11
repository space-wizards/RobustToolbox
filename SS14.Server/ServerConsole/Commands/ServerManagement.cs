using SS14.Server.Interfaces;
using SS14.Server.Interfaces.ServerConsole;
using SS14.Shared.IoC;

namespace SS14.Server.ServerConsole.Commands
{
    public class RestartServer : IConsoleCommand
    {
        public string Command => "restart";
        public string Description => "Restarts the server";
        public string Help => "Restarts the server.";

        public void Execute(params string[] args)
        {
            IoCManager.Resolve<IBaseServer>().Restart();
        }
    }

    // Crashes for some reason.
    public class StopServer : IConsoleCommand
    {
        public string Command => "shutdown";
        public string Description => "Stops the server";
        public string Help => "Stops the server brutally without telling clients.";

        public void Execute(params string[] args)
        {
            IoCManager.Resolve<IBaseServer>().Shutdown();
        }
    }
}
