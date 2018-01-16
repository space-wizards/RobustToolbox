using System;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.Enums;
using SS14.Shared.IoC;

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

    class JoinLobbyCommand : IClientCommand
    {
        public string Command => "joinlobby";
        public string Description => "Moves the player from the game to the lobby.";
        public string Help => String.Empty;

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            if (player.Status == SessionStatus.InGame)
                player.JoinLobby();
        }
    }

    class StartRoundCommand : IClientCommand
    {
        public string Command => "startround";
        public string Description => "Ends PreGame state and starts the round.";
        public string Help => String.Empty;

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            var baseServer = IoCManager.Resolve<IBaseServer>();

            if (baseServer.RunLevel == ServerRunLevel.PreGame)
            {
                baseServer.RunLevel = ServerRunLevel.Game;
            }
        }
    }

    class EndRoundCommand : IClientCommand
    {
        public string Command => "endround";
        public string Description => "Ends the round and moves the server to PostGame.";
        public string Help => String.Empty;

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            var baseServer = IoCManager.Resolve<IBaseServer>();

            if (baseServer.RunLevel == ServerRunLevel.Game)
            {
                baseServer.RunLevel = ServerRunLevel.PostGame;
            }
        }
    }

    class NewRoundCommand : IClientCommand
    {
        public string Command => "newround";
        public string Description => "Moves the server from PostRound to a new PreRound.";
        public string Help => String.Empty;

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            var baseServer = IoCManager.Resolve<IBaseServer>();

            if (baseServer.RunLevel == ServerRunLevel.PostGame)
            {
                baseServer.RunLevel = ServerRunLevel.MapChange;
            }
        }
    }
}
