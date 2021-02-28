using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Robust.Server.Console.Commands
{
    internal sealed class ReloadLocalizationsCommand : IConsoleCommand
    {
        public string Command => "rldloc";
        public string Description => "Reloads localization (client & server)";
        public string Help => "Usage: rldloc";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            IoCManager.Resolve<ILocalizationManager>().ReloadLocalizations();
        }
    }
}
