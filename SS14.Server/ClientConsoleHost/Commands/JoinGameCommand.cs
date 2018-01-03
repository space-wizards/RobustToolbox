using System;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.Player;
using SS14.Shared;

namespace SS14.Server.ClientConsoleHost.Commands
{
    class JoinGameCommand : IClientCommand
    {
        public string Command => "joingame";
        public string Description => "Moves the player from the lobby to the game.";
        public string Help => String.Empty;

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            if (player.Status == SessionStatus.InLobby)
                player.JoinGame();
        }
    }
}
