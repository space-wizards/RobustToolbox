using System;
using System.Collections.Generic;
using SS14.Server.Player;
using SS14.Shared;
using SS14.Shared.Enums;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Map;
using SS14.Shared.Players;

namespace SS14.Server.Interfaces.Player
{
    /// <summary>
    ///     Manages each players session when connected to the server.
    /// </summary>
    public interface IPlayerManager
    {
        /// <summary>
        ///     Number of players currently connected to this server.
        /// </summary>
        int PlayerCount { get; }

        string PlayerPrototypeName { get; set; }

        /// <summary>
        ///     Maximum number of players that can connect to this server at one time.
        /// </summary>
        int MaxPlayers { get; }

        /// <summary>
        ///     Fallback spawn point to use if map does not provide it.
        /// </summary>
        LocalCoordinates FallbackSpawnPoint { get; set; }

        /// <summary>
        ///     Raised when the <see cref="SessionStatus"/> of a <see cref="IPlayerSession"/> is changed.
        /// </summary>
        event EventHandler<SessionStatusEventArgs> PlayerStatusChanged;

        /// <summary>
        ///     Initializes the manager.
        /// </summary>
        /// <param name="baseServer">The server that instantiated this manager.</param>
        /// <param name="maxPlayers">Maximum number of players that can connect to this server at one time.</param>
        void Initialize(BaseServer baseServer, int maxPlayers);

        IPlayerSession GetSessionById(PlayerIndex session);

        IPlayerSession GetSessionByChannel(INetChannel channel);

        /// <summary>
        ///     Checks to see if a PlayerIndex is a valid session.
        /// </summary>
        bool ValidSessionId(PlayerIndex index);

        //TODO: Move to IPlayerSession
        void SpawnPlayerMob(IPlayerSession session);
        
        void SendJoinGameToAll();
        void SendJoinLobbyToAll();
        
        void DetachAll();
        List<IPlayerSession> GetPlayersInLobby();
        List<IPlayerSession> GetPlayersInRange(LocalCoordinates worldPos, int range);
        List<IPlayerSession> GetAllPlayers();
        List<PlayerState> GetPlayerStates();
    }
}
