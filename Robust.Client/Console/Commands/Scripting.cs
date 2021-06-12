using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Robust.Client.Console.Commands
{
#if CLIENT_SCRIPTING
    internal sealed class ScriptCommand : IConsoleCommand
    {
        public string Command => "csi";
        public string Description => Loc.GetString("console-script-command-description");
        public string Help => "csi";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            new ScriptConsoleClient().OpenCentered();
        }
    }

    internal sealed class WatchCommand : IConsoleCommand
    {
        public string Command => "watch";
        public string Description => Loc.GetString("console-watch-command-description");
        public string Help => "watch";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            new WatchWindow().OpenCentered();
        }
    }
#endif

    internal sealed class ServerScriptCommand : IConsoleCommand
    {
        public string Command => "scsi";
        public string Description => Loc.GetString("console-server-script-command-description");
        public string Help => "scsi";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mgr = IoCManager.Resolve<IScriptClient>();
            if (!mgr.CanScript)
            {
                shell.WriteError(Loc.GetString("console-server-script-command-cannot-script"));
                return;
            }

            mgr.StartSession();
        }
    }
}
