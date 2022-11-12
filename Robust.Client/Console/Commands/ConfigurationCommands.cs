using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    [UsedImplicitly]
    public sealed class SaveConfig : LocalizedCommands
    {
        public override string Command => "saveconfig";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            IoCManager.Resolve<IConfigurationManager>().SaveToFile();
        }
    }

}
