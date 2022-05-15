using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    [UsedImplicitly]
    public sealed class SaveConfig : IConsoleCommand
    {
        public string Command => "saveconfig";
        public string Description => "Saves the client configuration to the config file";
        public string Help => "saveconfig";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            IoCManager.Resolve<IConfigurationManager>().SaveToFile();
        }
    }

}
