using SS14.Server.Interfaces.Configuration;
using SS14.Shared.IoC;

namespace SS14.Server.Services.ServerConsole.Commands
{
    public class SaveConfig : ConsoleCommand
    {
        public override string Command
        {
            get { return "saveconfig"; }
        }

        public override string Description
        {
            get { return "Saves the server configuration to the config file"; }
        }

        public override string Help
        {
            get { return "No arguments required. Saves the server configuration to the config file."; }
        }

        public override void Execute(params string[] args)
        {
            IoCManager.Resolve<IServerConfigurationManager>().Save();
        }
    }
}