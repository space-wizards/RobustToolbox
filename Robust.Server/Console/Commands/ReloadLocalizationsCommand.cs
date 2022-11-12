using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Robust.Server.Console.Commands
{
    internal sealed class ReloadLocalizationsCommand : LocalizedCommands
    {
        public override string Command => "rldloc";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            IoCManager.Resolve<ILocalizationManager>().ReloadLocalizations();
        }
    }
}
