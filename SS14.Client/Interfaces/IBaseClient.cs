using System;

namespace SS14.Client.Interfaces
{
    /// <summary>
    ///     Top level class that controls the game logic of the client.
    /// </summary>
    public interface IBaseClient : IDisposable
    {
        /// <summary>
        ///     Default port that the client tries to connect to if no other port is specified.
        /// </summary>
        ushort DefaultPort { get; }

        /// <summary>
        ///     Current RunLevel that the client is at.
        /// </summary>
        ClientRunLevel RunLevel { get; }

        /// <summary>
        ///     Various bits of config info received when setting up a session.
        /// </summary>
        //TODO: Move this to the CVar system?
        ServerInfo GameInfo { get; }

        /// <summary>
        ///     Raised when the client RunLevel is changed.
        /// </summary>
        event EventHandler<RunLevelChangedEventArgs> RunLevelChanged;

        /// <summary>
        ///     Raised when the player successfully joins the server.
        /// </summary>
        event EventHandler<PlayerEventArgs> PlayerJoinedServer;

        /// <summary>
        ///     Raised when the player switches to the lobby.
        /// </summary>
        event EventHandler<PlayerEventArgs> PlayerJoinedLobby;

        /// <summary>
        ///     Raised when the player switches to the game.
        /// </summary>
        event EventHandler<PlayerEventArgs> PlayerJoinedGame;

        /// <summary>
        ///     Raised right before the player leaves the server.
        /// </summary>
        event EventHandler<PlayerEventArgs> PlayerLeaveServer;

        /// <summary>
        ///     Call this after BaseClient has been created. This sets up the object to its initial state. Only call this once.
        /// </summary>
        void Initialize();

        /// <summary>
        ///     Called every frame outside the simulation. DO NOT update the simulation from this function.
        /// </summary>
        void Update();

        /// <summary>
        ///     Called every tick inside the simulation. See IGameTiming for more info about the tick.
        /// </summary>
        void Tick();

        /// <summary>
        ///     Connects the Initialized BaseClient to a remote server.
        /// </summary>
        void ConnectToServer(string ip, ushort port);

        /// <summary>
        ///     Disconnects the connected BaseClient from a remote server.
        /// </summary>
        void DisconnectFromServer(string reason);
    }
}
