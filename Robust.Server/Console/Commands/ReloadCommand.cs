using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Robust.Server.Console.Commands
{
    [UsedImplicitly]
    public class ReloadCommand : IConsoleCommand
    {
        public string Command => "reload";
        public string Description => "Reloads all entity prototypes and updates entities in-game accordingly";
        public string Help => "";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
#if !FULL_RELEASE
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            prototypeManager.ReloadPrototypes();
#else
            shell.WriteLine("Not supported on full release.");
#endif
        }
    }
}
