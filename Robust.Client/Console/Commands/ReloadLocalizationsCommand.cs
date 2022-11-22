using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Robust.Client.Console.Commands
{
    internal sealed class ReloadLocalizationsCommand : LocalizedCommands
    {
        [Dependency] private readonly ILocalizationManager _loc = default!;

        public override string Command => "rldloc";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _loc.ReloadLocalizations();

            shell.RemoteExecuteCommand("sudo rldloc");
        }
    }
}
