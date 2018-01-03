using SS14.Server.Interfaces;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.Player;
using SS14.Shared.IoC;

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
}
