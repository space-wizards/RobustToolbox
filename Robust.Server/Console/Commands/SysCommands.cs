using System.Text;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Robust.Server.Console.Commands
{
    sealed class RestartCommand : LocalizedCommands
    {
        public override string Command => "restart";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            IoCManager.Resolve<IBaseServer>().Restart();
        }
    }

    sealed class ShutdownCommand : LocalizedCommands
    {
        public override string Command => "shutdown";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            IoCManager.Resolve<IBaseServer>().Shutdown(null);
        }
    }

    public sealed class SaveConfig : LocalizedCommands
    {
        public override string Command => "saveconfig";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            IoCManager.Resolve<IConfigurationManager>().SaveToFile();
        }
    }

    sealed class NetworkAuditCommand : LocalizedCommands
    {
        public override string Command => "netaudit";
        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var network = (NetManager) IoCManager.Resolve<INetManager>();

            var callbacks = network.CallbackAudit;

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

    sealed class ShowTimeCommand : LocalizedCommands
    {
        public override string Command => "showtime";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var timing = IoCManager.Resolve<IGameTiming>();
            shell.WriteLine($"Paused: {timing.Paused}, CurTick: {timing.CurTick}, CurTime: {timing.CurTime}, RealTime: {timing.RealTime}");
        }
    }
}
