namespace SS14.Server.Interfaces
{
    /// <summary>
    /// The base server that controls the engine. This is managed by the GameServer.
    /// </summary>
    public interface IBaseServer
    {
        /// <summary>
        /// Saves the current running game to disk.
        /// </summary>
        void SaveGame();

        /// <summary>
        /// Sets up the server, loads the game, gets ready for client connections.
        /// </summary>
        /// <returns></returns>
        bool Start();

        /// <summary>
        /// Hard restarts the server, shutting it down, kicking all players, and starting the server again.
        /// </summary>
        void Restart();

        /// <summary>
        /// Shuts down the server, and ends the process.
        /// </summary>
        /// <param name="reason">Reason why the server was shut down.</param>
        void Shutdown(string reason = null);

        /// <summary>
        /// Enters the main loop of the server. This functions blocks until the server is shut down.
        /// </summary>
        void MainLoop();

        /// <summary>
        /// The Run Level of the server has changed.
        /// </summary>
        event EventRunLevelChanged OnRunLevelChanged;

        /// <summary>
        /// The local clock has ticked.
        /// </summary>
        event EventTick OnTick;
    }
}
