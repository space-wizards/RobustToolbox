using System;
using System.Net;

namespace Robust.Client
{
    /// <summary>
    ///     Top level class that controls the game logic of the client.
    /// </summary>
    public interface IBaseClient
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
        ServerInfo? GameInfo { get; }

        /// <summary>
        /// A player name to use when connecting to the server instead of the one found in the configuration.
        /// </summary>
        string? PlayerNameOverride { get; set; }

        string? LastDisconnectReason { get; }

        /// <summary>
        ///     Raised when the client RunLevel is changed.
        /// </summary>
        event EventHandler<RunLevelChangedEventArgs> RunLevelChanged;

        /// <summary>
        ///     Raised when the player successfully joins the server.
        /// </summary>
        event EventHandler<PlayerEventArgs> PlayerJoinedServer;

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
        ///     Connects the Initialized BaseClient to a remote server.
        /// </summary>
        void ConnectToServer(string ip, ushort port)
        {
            ConnectToServer(new DnsEndPoint(ip, port));
        }

        void ConnectToServer(DnsEndPoint endPoint);

        /// <summary>
        ///     Disconnects the connected BaseClient from a remote server.
        /// </summary>
        void DisconnectFromServer(string reason);

        /// <summary>
        ///     Starts the single player mode.
        /// </summary>
        void StartSinglePlayer();

        /// <summary>
        ///     Stops the single player mode.
        /// </summary>
        void StopSinglePlayer();
    }
}
