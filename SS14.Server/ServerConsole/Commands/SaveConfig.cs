using SS14.Shared.Interfaces.Configuration;
using SS14.Server.Interfaces.ServerConsole;
using SS14.Shared.IoC;

namespace SS14.Server.ServerConsole.Commands
{
    public class SaveConfig : IConsoleCommand
    {
        public string Command => "saveconfig";
        public string Description => "Saves the server configuration to the config file";
        public string Help => "No arguments required. Saves the server configuration to the config file.";

        public void Execute(params string[] args)
        {
            IoCManager.Resolve<IConfigurationManager>().SaveFile();
        }
    }
}
