using SS14.Server.Interfaces.Configuration;
using SS14.Shared.IoC;

namespace SS14.Server.Services.ServerConsole.Commands
{
    public class SaveConfig : ConsoleCommand
    {
        public override string Command => "saveconfig";
        public override string Description => "Saves the server configuration to the config file";
        public override string Help => "No arguments required. Saves the server configuration to the config file.";

        public override void Execute(params string[] args)
        {
            IoCManager.Resolve<IServerConfigurationManager>().Save();
        }
    }
}
