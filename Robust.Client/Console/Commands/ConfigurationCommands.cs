using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    [UsedImplicitly]
    public sealed class SaveConfig : LocalizedCommands
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;


        public override string Command => "saveconfig";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _cfg.SaveToFile();
        }
    }

}
