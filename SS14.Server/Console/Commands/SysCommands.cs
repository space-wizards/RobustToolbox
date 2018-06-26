using System.Text;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Network;

namespace SS14.Server.ClientConsoleHost.Commands
{
    class RestartCommand : IClientCommand
    {
        public string Command => "restart";
        public string Description => "Gracefully restarts the server (not just the round).";
        public string Help => "restart";
        public void Execute(IClientConsoleHost host, IPlayerSession player, string[] args)
        {
            IoCManager.Resolve<IBaseServer>().Restart();
        }
    }

    class ShutdownCommand : IClientCommand
    {
        public string Command => "shutdown";
        public string Description => "Gracefully shuts down the server.";
        public string Help => "shutdown";
        public void Execute(IClientConsoleHost host, IPlayerSession player, string[] args)
        {
            IoCManager.Resolve<IBaseServer>().Shutdown();
        }
    }
    
    public class SaveConfig : IClientCommand
    {
        public string Command => "saveconfig";
        public string Description => "Saves the server configuration to the config file";
        public string Help => "saveconfig";

        public void Execute(IClientConsoleHost host, IPlayerSession player, string[] args)
        {
            IoCManager.Resolve<IConfigurationManager>().SaveToFile();
        }
    }

    class NetworkAuditCommand : IClientCommand
    {
        public string Command => "netaudit";
        public string Description => "Prints into about NetMsg security.";
        public string Help => "netaudit";
        public void Execute(IClientConsoleHost host, IPlayerSession player, string[] args)
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

            host.SendText(player, sb.ToString());
        }
    }

    class HelpCommand : IClientCommand
    {
        public string Command => "help";
        public string Description => "When no arguments are provided, displays a generic help text. When an argument is passed, display the help text for the command with that name.";
        public string Help => "Help";
        public void Execute(IClientConsoleHost host, IPlayerSession player, string[] args)
        {
            switch (args.Length)
            {
                case 0:
                    host.SendText(player, "To display help for a specific command, write 'help <command>'. To list all available commands, write 'list'.");
                    break;

                case 1:
                    var commandName = args[0];
                    if (!host.AvailableCommands.TryGetValue(commandName, out var cmd))
                    {
                        host.SendText(player, $"Unknown command: {commandName}");
                        return;
                    }

                    host.SendText(player, $"Use: {cmd.Help}\n{cmd.Description}");
                    break;

                default:
                    host.SendText(player, "Invalid amount of arguments.");
                    break;
            }
        }
    }
}
