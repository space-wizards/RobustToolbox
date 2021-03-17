using System;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Robust.Server
{
    /// <summary>
    ///     Top level class that controls the game logic of the server.
    /// </summary>
    public interface IBaseServer
    {
        /// <summary>
        ///     The maximum number of players allowed in the server.
        /// </summary>
        int MaxPlayers { get; }

        /// <summary>
        ///     The displayed name of our server.
        /// </summary>
        string ServerName { get; }

        /// <summary>
        ///     Sets up the server, loads the game, gets ready for client connections.
        /// </summary>
        /// <returns></returns>
        bool Start(Func<ILogHandler>? logHandler = null);

        /// <summary>
        ///     Hard restarts the server, shutting it down, kicking all players, and starting the server again.
        /// </summary>
        void Restart();

        /// <summary>
        ///     Shuts down the server, and ends the process.
        /// </summary>
        /// <param name="reason">Reason why the server was shut down.</param>
        void Shutdown(string? reason);

        /// <summary>
        ///     Enters the main loop of the server. This functions blocks until the server is shut down.
        /// </summary>
        void MainLoop();
    }

    internal interface IBaseServerInternal : IBaseServer
    {
        bool DisableLoadContext { set; }
        bool LoadConfigAndUserData { set; }

        void OverrideMainLoop(IGameLoop gameLoop);

        void SetCommandLineArgs(CommandLineArgs args);
    }
}
