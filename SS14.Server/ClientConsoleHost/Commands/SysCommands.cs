using System.Text;
using System.Xml;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Network;

namespace SS14.Server.ClientConsoleHost.Commands
{
    class SaveCommand : IClientCommand
    {
        public string Command => "save";
        public string Description => "Saves the current map to disk.";
        public string Help => "save";

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            //TODO: Check permissions here.
            IoCManager.Resolve<IBaseServer>().SaveGame();
        }
    }

    class RestartCommand : IClientCommand
    {
        public string Command => "restart";
        public string Description => "restarts the current round.";
        public string Help => "restart";
        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            //TODO: Check permissions here.
            IoCManager.Resolve<IBaseServer>().Restart();
        }
    }

    class NetworkAuditCommand : IClientCommand
    {
        public string Command => "netaudit";
        public string Description => "Prints into about NetMsg security.";
        public string Help => "netaudit";
        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            var network = (NetManager)IoCManager.Resolve<INetManager>();

            var callbacks = network.CallbackAudit;

            foreach (var kvCallback in callbacks)
            {
                var msgType = kvCallback.Key;
                var call = kvCallback.Value;

                var str = $"Type: {msgType.Name} Call:{call.Target}";

                host.SendConsoleReply(player.ConnectedClient, str);
            }
        }
    }
}
