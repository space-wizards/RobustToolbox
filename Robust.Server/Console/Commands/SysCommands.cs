using System.Text;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Robust.Server.Console.Commands
{
    /*
    // Disabled for now since it doesn't actually work.
    sealed class RestartCommand : LocalizedCommands
    {
        [Dependency] private IBaseServer _server = default!;

        public override string Command => "restart";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _server.Restart();
        }
    }
    */

    sealed partial class ShutdownCommand : LocalizedCommands
    {
        [Dependency] private IBaseServer _server = default!;

        public override string Command => "shutdown";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            switch (args.Length)
            {
                case > 1:
                    shell.WriteError(Loc.GetString("shell-need-between-arguments", ("lower", 0), ("upper", 1), ("currentAmount", args.Length)));
                    break;
                case 1:
                    _server.Shutdown(args[0]);
                    break;
                default:
                    _server.Shutdown();
                    break;
            }
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    return CompletionResult.FromHint(Loc.GetString("cmd-shutdown-hint-1"));
                default:
                    return CompletionResult.Empty;
            }
        }
    }

    sealed partial class NetworkAuditCommand : LocalizedCommands
    {
        [Dependency] private INetManager _netManager = default!;

        public override string Command => "netaudit";
        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var callbacks = ((NetManager)_netManager).CallbackAudit;

            var sb = new StringBuilder();

            foreach (var kvCallback in callbacks)
            {
                var msgType = kvCallback.Key;
                var call = kvCallback.Value;

                sb.AppendLine($"Type: {msgType.Name.PadRight(16)} Call:{call.Target}");
            }

            shell.WriteLine(sb.ToString());
        }
    }

    sealed partial class ShowTimeCommand : LocalizedCommands
    {
        [Dependency] private IGameTiming _timing = default!;

        public override string Command => "showtime";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            shell.WriteLine($"Paused: {_timing.Paused}, CurTick: {_timing.CurTick}, CurTime: {_timing.CurTime}, RealTime: {_timing.RealTime}");
        }
    }
}
