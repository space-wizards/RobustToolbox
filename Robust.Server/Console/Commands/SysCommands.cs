using System.Text;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Robust.Server.Console.Commands
{
    sealed class RestartCommand : IConsoleCommand
    {
        public string Command => "restart";
        public string Description => "Gracefully restarts the server (not just the round).";
        public string Help => "restart";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            IoCManager.Resolve<IBaseServer>().Restart();
        }
    }

    sealed class ShutdownCommand : IConsoleCommand
    {
        public string Command => "shutdown";
        public string Description => "Gracefully shuts down the server.";
        public string Help => "shutdown";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            IoCManager.Resolve<IBaseServer>().Shutdown(null);
        }
    }

    public sealed class SaveConfig : IConsoleCommand
    {
        public string Command => "saveconfig";
        public string Description => "Saves the server configuration to the config file";
        public string Help => "saveconfig";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            IoCManager.Resolve<IConfigurationManager>().SaveToFile();
        }
    }

    sealed class NetworkAuditCommand : IConsoleCommand
    {
        public string Command => "netaudit";
        public string Description => "Prints into about NetMsg security.";
        public string Help => "netaudit";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
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

    sealed class ShowTimeCommand : IConsoleCommand
    {
        public string Command => "showtime";
        public string Description => "Shows the server time.";
        public string Help => "showtime";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var timing = IoCManager.Resolve<IGameTiming>();
            shell.WriteLine($"Paused: {timing.Paused}, CurTick: {timing.CurTick}, CurTime: {timing.CurTime}, RealTime: {timing.RealTime}");
        }
    }

    internal sealed class SerializeStatsCommand : IConsoleCommand
    {

        public string Command => "szr_stats";

        public string Description => "Report serializer statistics.";

        public string Help => "szr_stats";

        public void Execute(IConsoleShell console, string argStr, string[] args)
        {

            console.WriteLine($"serialized: {RobustSerializer.BytesSerialized} bytes, {RobustSerializer.ObjectsSerialized} objects");
            console.WriteLine($"largest serialized: {RobustSerializer.LargestObjectSerializedBytes} bytes, {RobustSerializer.LargestObjectSerializedType} objects");
            console.WriteLine($"deserialized: {RobustSerializer.BytesDeserialized} bytes, {RobustSerializer.ObjectsDeserialized} objects");
            console.WriteLine($"largest serialized: {RobustSerializer.LargestObjectDeserializedBytes} bytes, {RobustSerializer.LargestObjectDeserializedType} objects");
        }

    }
}
