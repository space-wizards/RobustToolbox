using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Robust.Client.Console.Commands
{
#if CLIENT_SCRIPTING
    internal sealed class ScriptCommand : IClientCommand
    {
        public string Command => "csi";
        public string Description => "Opens a C# interactive console.";
        public string Help => "csi";

        public void Execute(IClientConsoleShell shell, string argStr, string[] args)
        {
            new ScriptConsoleClient().OpenCentered();
        }
    }

    internal sealed class WatchCommand : IClientCommand
    {
        public string Command => "watch";
        public string Description => "Opens a variable watch window.";
        public string Help => "watch";

        public void Execute(IClientConsoleShell shell, string argStr, string[] args)
        {
            new WatchWindow().OpenCentered();
        }
    }
#endif

    internal sealed class ServerScriptCommand : IClientCommand
    {
        public string Command => "scsi";
        public string Description => "Opens a C# interactive console on the server.";
        public string Help => "scsi";

        public void Execute(IClientConsoleShell shell, string argStr, string[] args)
        {
            var mgr = IoCManager.Resolve<IScriptClient>();
            if (!mgr.CanScript)
            {
                shell.WriteLine(Loc.GetString("You do not have server side scripting permission."), Color.Red);
                return;
            }

            mgr.StartSession();
        }
    }
}
