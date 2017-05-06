using SS14.Server.Interfaces;
using SS14.Shared.IoC;
using System;

namespace SS14.Server.Services.ServerConsole.Commands
{
    public class RestartServer : ConsoleCommand
    {
        public override string Command => "restartserver";
        public override string Description => "Restarts the server";
        public override string Help => "Restarts the server.";

        public override void Execute(params string[] args)
        {
            IoCManager.Resolve<ISS14Server>().Restart();
        }
    }

    // Crashes for some reason.
    public class StopServer : ConsoleCommand
    {
        public override string Command => "stop";
        public override string Description => "Stops the server";
        public override string Help => "Stops the server brutally without telling clients.";

        public override void Execute(params string[] args)
        {
            Environment.Exit(0);
        }
    }
}
