using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Robust.Client.Console.Commands
{
#if CLIENT_SCRIPTING
    internal sealed class ScriptCommand : IConsoleCommand
    {
        public string Command => "csi";
        public string Description => "Opens a C# interactive console.";
        public string Help => "csi";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            new ScriptConsoleClient().OpenCentered();
        }
    }

    internal sealed class WatchCommand : IConsoleCommand
    {
        public string Command => "watch";
        public string Description => "Opens a variable watch window.";
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
        public string Description => "Opens a C# interactive console on the server.";
        public string Help => "scsi";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mgr = IoCManager.Resolve<IScriptClient>();
            if (!mgr.CanScript)
            {
                shell.WriteError(Loc.GetString("You do not have server side scripting permission."));
                return;
            }

            mgr.StartSession();
        }
    }
}
