using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Robust.Server.Console.Commands
{
    [InjectDependencies]
    internal sealed partial class ReloadLocalizationsCommand : LocalizedCommands
    {
        [Dependency] private ILocalizationManager _loc = default!;

        public override string Command => "rldloc";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _loc.ReloadLocalizations();
        }
    }
}
