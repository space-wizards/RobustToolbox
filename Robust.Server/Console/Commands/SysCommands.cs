using System.Text;
using Robust.Server.Interfaces;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Robust.Server.Console.Commands
{
    class RestartCommand : IClientCommand
    {
        public string Command => "restart";
        public string Description => "Gracefully restarts the server (not just the round).";
        public string Help => "restart";
        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            IoCManager.Resolve<IBaseServer>().Restart();
        }
    }

    class ShutdownCommand : IClientCommand
    {
        public string Command => "shutdown";
        public string Description => "Gracefully shuts down the server.";
        public string Help => "shutdown";
        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            IoCManager.Resolve<IBaseServer>().Shutdown(null);
        }
    }

    public class SaveConfig : IClientCommand
    {
        public string Command => "saveconfig";
        public string Description => "Saves the server configuration to the config file";
        public string Help => "saveconfig";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            IoCManager.Resolve<IConfigurationManager>().SaveToFile();
        }
    }

    class NetworkAuditCommand : IClientCommand
    {
        public string Command => "netaudit";
        public string Description => "Prints into about NetMsg security.";
        public string Help => "netaudit";
        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var network = (NetManager)IoCManager.Resolve<INetManager>();

            var callbacks = network.CallbackAudit;

            var sb = new StringBuilder();

            foreach (var kvCallback in callbacks)
            {
                var msgType = kvCallback.Key;
                var call = kvCallback.Value;

                sb.AppendLine($"Type: {msgType.Name.PadRight(16)} Call:{call.Target}");
            }

            shell.SendText(player, sb.ToString());
        }
    }

    class HelpCommand : IClientCommand
    {
        public string Command => "help";
        public string Description => "When no arguments are provided, displays a generic help text. When an argument is passed, display the help text for the command with that name.";
        public string Help => "Help";
        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            switch (args.Length)
            {
                case 0:
                    shell.SendText(player, "To display help for a specific command, write 'help <command>'. To list all available commands, write 'list'.");
                    break;

                case 1:
                    var commandName = args[0];
                    if (!shell.AvailableCommands.TryGetValue(commandName, out var cmd))
                    {
                        shell.SendText(player, $"Unknown command: {commandName}");
                        return;
                    }

                    shell.SendText(player, $"Use: {cmd.Help}\n{cmd.Description}");
                    break;

                default:
                    shell.SendText(player, "Invalid amount of arguments.");
                    break;
            }
        }
    }

    class ShowTimeCommand : IClientCommand
    {
        public string Command => "showtime";
        public string Description => "Shows the server time.";
        public string Help => "showtime";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var timing = IoCManager.Resolve<IGameTiming>();
            shell.SendText(player, $"Paused: {timing.Paused}, CurTick: {timing.CurTick}, CurTime: {timing.CurTime}, RealTime: {timing.RealTime}");
        }
    }
}
