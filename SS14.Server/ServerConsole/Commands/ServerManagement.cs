using SS14.Server.Interfaces;
using SS14.Server.Interfaces.ServerConsole;
using SS14.Shared.IoC;
using System;

namespace SS14.Server.Services.ServerConsole.Commands
{
    public class RestartServer : IConsoleCommand
    {
        public string Command => "restartserver";
        public string Description => "Restarts the server";
        public string Help => "Restarts the server.";

        public void Execute(params string[] args)
        {
            IoCManager.Resolve<ISS14Server>().Restart();
        }
    }

    // Crashes for some reason.
    public class StopServer : IConsoleCommand
    {
        public string Command => "stop";
        public string Description => "Stops the server";
        public string Help => "Stops the server brutally without telling clients.";

        public  void Execute(params string[] args)
        {
            Environment.Exit(0);
        }
    }
}
