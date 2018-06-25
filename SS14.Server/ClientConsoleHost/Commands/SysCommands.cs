using System.Text;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Network;

namespace SS14.Server.ClientConsoleHost.Commands
{
    class SvRestartCommand : IClientCommand
    {
        public string Command => "sv_restart";
        public string Description => "Gracefully restarts the server (not just the round).";
        public string Help => "sv_restart";
        public void Execute(IClientConsoleHost host, IPlayerSession player, string[] args)
        {
            IoCManager.Resolve<IBaseServer>().Restart();
        }
    }

    class SvShutdownCommand : IClientCommand
    {
        public string Command => "sv_shutdown";
        public string Description => "Gracefully shuts down the server.";
        public string Help => "sv_shutdown";
        public void Execute(IClientConsoleHost host, IPlayerSession player, string[] args)
        {
            IoCManager.Resolve<IBaseServer>().Shutdown();
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
}
