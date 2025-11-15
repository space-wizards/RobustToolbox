using Robust.Client.UserInterface;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
#if CLIENT_SCRIPTING
    internal sealed class ScriptCommand : LocalizedCommands
    {
        [Dependency] private readonly IUserInterfaceManagerInternal _uiManager = default!;

        public override string Command => "csi";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            ((UserInterfaceManager)_uiManager).DebugConsole.MainControl.AddPanel(new ScriptConsoleClient());
        }
    }

    internal sealed class WatchCommand : LocalizedCommands
    {
        public override string Command => "watch";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            new WatchWindow().OpenCentered();
        }
    }
#endif

    internal sealed class ServerScriptCommand : LocalizedCommands
    {
        [Dependency] private readonly IScriptClient _scriptClient = default!;

        public override string Command => "scsi";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (!_scriptClient.CanScript)
            {
                shell.WriteError("You do not have server side scripting permission.");
                return;
            }

            _scriptClient.StartSession();
        }
    }
}
