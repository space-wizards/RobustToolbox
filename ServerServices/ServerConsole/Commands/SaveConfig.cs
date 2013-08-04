using SS13.IoC;
using ServerInterfaces.Configuration;

namespace ServerServices.ServerConsole.Commands
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
            IoCManager.Resolve<IConfigurationManager>().Save();
        }
    }
}