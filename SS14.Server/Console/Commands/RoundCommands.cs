using System;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Console;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Enums;
using SS14.Shared.IoC;

namespace SS14.Server.Console.Commands
{
    class JoinGameCommand : IClientCommand
    {
        public string Command => "joingame";
        public string Description => "Moves the player from the lobby to the game.";
        public string Help => String.Empty;

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player.Status == SessionStatus.InLobby)
                player.JoinGame();
        }
    }

    class JoinLobbyCommand : IClientCommand
    {
        public string Command => "joinlobby";
        public string Description => "Moves the player from the game to the lobby.";
        public string Help => String.Empty;

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player.Status == SessionStatus.InGame)
                player.JoinLobby();
        }
    }
}
