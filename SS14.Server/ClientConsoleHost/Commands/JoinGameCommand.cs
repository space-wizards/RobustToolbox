using System;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;

namespace SS14.Server.ClientConsoleHost.Commands
{
    class JoinGameCommand : IClientCommand
    {
        public string Command => "joingame";
        public string Description => "Moves the player from the lobby to the game.";
        public string Help => String.Empty;

        public void Execute(IClientConsoleHost host, INetChannel client, params string[] args)
        {
            var players = IoCManager.Resolve<IPlayerManager>();
            var session = players.GetSessionByChannel(client);

            if(session.Status == SessionStatus.InLobby)
                session.JoinGame();
        }
    }
}
