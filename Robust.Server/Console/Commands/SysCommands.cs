using System;
using System.Runtime;
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

    sealed class HelpCommand : IConsoleCommand
    {
        public string Command => "help";

        public string Description =>
            "When no arguments are provided, displays a generic help text. When an argument is passed, display the help text for the command with that name.";

        public string Help => "Help";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            switch (args.Length)
            {
                case 0:
                    shell.WriteLine("To display help for a specific command, write 'help <command>'. To list all available commands, write 'list'.");
                    break;

                case 1:
                    var commandName = args[0];
                    if (!shell.ConsoleHost.RegisteredCommands.TryGetValue(commandName, out var cmd))
                    {
                        shell.WriteLine($"Unknown command: {commandName}");
                        return;
                    }

                    shell.WriteLine($"Use: {cmd.Help}\n{cmd.Description}");
                    break;

                default:
                    shell.WriteLine("Invalid amount of arguments.");
                    break;
            }
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

    internal sealed class GcCommand : IConsoleCommand
    {
        public string Command => "gc";
        public string Description => "Run the GC.";
        public string Help => "gc [generation]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length == 0)
            {
                GC.Collect();
            }
            else
            {
                if(int.TryParse(args[0], out int result))
                    GC.Collect(result);
                else
                    shell.WriteLine("Failed to parse argument.");
            }
        }
    }

    internal sealed class GcModeCommand : IConsoleCommand
    {

        public string Command => "gc_mode";

        public string Description => "Change the GC Latency mode.";

        public string Help => "gc_mode [type]";

        public void Execute(IConsoleShell console, string argStr, string[] args)
        {
            var prevMode = GCSettings.LatencyMode;
            if (args.Length == 0)
            {
                console.WriteLine($"current gc latency mode: {(int) prevMode} ({prevMode})");
                console.WriteLine("possible modes:");
                foreach (var mode in (int[]) Enum.GetValues(typeof(GCLatencyMode)))
                {
                    console.WriteLine($" {mode}: {Enum.GetName(typeof(GCLatencyMode), mode)}");
                }
            }
            else
            {
                GCLatencyMode mode;
                if (char.IsDigit(args[0][0]) && int.TryParse(args[0], out var modeNum))
                {
                    mode = (GCLatencyMode) modeNum;
                }
                else if (!Enum.TryParse(args[0], true, out mode))
                {
                    console.WriteLine($"unknown gc latency mode: {args[0]}");
                    return;
                }

                console.WriteLine($"attempting gc latency mode change: {(int) prevMode} ({prevMode}) -> {(int) mode} ({mode})");
                GCSettings.LatencyMode = mode;
                console.WriteLine($"resulting gc latency mode: {(int) GCSettings.LatencyMode} ({GCSettings.LatencyMode})");
            }

            return;
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

    internal sealed class MemCommand : IConsoleCommand
    {
        public string Command => "mem";
        public string Description => "prints memory info";
        public string Help => "mem";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
#if !NETCOREAPP
            shell.SendText(player, "Memory info is only available on .NET Core");
#else
            var info = GC.GetGCMemoryInfo();

            shell.WriteLine($@"Heap Size: {FormatBytes(info.HeapSizeBytes)} Total Allocated: {FormatBytes(GC.GetTotalMemory(false))}");
#endif
        }

#if NETCOREAPP
        private static string FormatBytes(long bytes)
        {
            return $"{bytes / 1024} KiB";
        }
#endif
    }
}
