using System;
using SS14.Server.Player;

namespace SS14.Server.Interfaces
{
    /// <summary>
    ///     Top level class that controls the game logic of the server.
    /// </summary>
    public interface IBaseServer
    {
        /// <summary>
        ///     Current RunLevel that the server is at.
        /// </summary>
        ServerRunLevel RunLevel { get; set; }

        /// <summary>
        ///     The name of the current running map.
        /// </summary>
        string MapName { get; }

        /// <summary>
        ///     The maximum number of players allowed in the server.
        /// </summary>
        int MaxPlayers { get; }

        /// <summary>
        ///     The displayed name of our server.
        /// </summary>
        string ServerName { get; }

        /// <summary>
        ///     The MOTD displayed when joining the server.
        /// </summary>
        string Motd { get; }

        /// <summary>
        ///     The name of the game mode displayed to clients.
        /// </summary>
        string GameModeName { get; set; }

        /// <summary>
        ///     Saves the current running game to disk.
        /// </summary>
        void SaveGame();

        /// <summary>
        ///     Sets up the server, loads the game, gets ready for client connections.
        /// </summary>
        /// <returns></returns>
        bool Start();

        /// <summary>
        ///     Hard restarts the server, shutting it down, kicking all players, and starting the server again.
        /// </summary>
        void Restart();

        /// <summary>
        ///     Shuts down the server, and ends the process.
        /// </summary>
        /// <param name="reason">Reason why the server was shut down.</param>
        void Shutdown(string reason = null);

        /// <summary>
        ///     Enters the main loop of the server. This functions blocks until the server is shut down.
        /// </summary>
        void MainLoop();

        /// <summary>
        ///     Raised when the server RunLevel is changed.
        /// </summary>
        event EventHandler<RunLevelChangedEventArgs> RunLevelChanged;
        
        /// <summary>
        ///     Raised when a player successfully joins the server.
        /// </summary>
        event EventHandler<PlayerEventArgs> PlayerJoinedServer;

        /// <summary>
        ///     Raised when a player switches to the lobby.
        /// </summary>
        event EventHandler<PlayerEventArgs> PlayerJoinedLobby;

        /// <summary>
        ///     Raised when a player switches to the game.
        /// </summary>
        event EventHandler<PlayerEventArgs> PlayerJoinedGame;

        /// <summary>
        ///     Raised right before a player leaves the server.
        /// </summary>
        event EventHandler<PlayerEventArgs> PlayerLeaveServer;
    }
}
