using System;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;

namespace SS14.Server.ClientConsoleHost.Commands
{
    class SaveCommand : IClientCommand
    {
        public string Command => "save";
        public string Description => "Saves the current map to disk.";
        public string Help => String.Empty;

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            IoCManager.Resolve<IBaseServer>().SaveGame();
        }
    }
}
