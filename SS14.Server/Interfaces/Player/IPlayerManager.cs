using System;
using System.Collections.Generic;
using SS14.Server.Player;
using SS14.Shared.Enums;
using SS14.Shared.GameStates;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Map;
using SS14.Shared.Network;
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

        BoundKeyMap KeyMap { get; }

        /// <summary>
        ///     Maximum number of players that can connect to this server at one time.
        /// </summary>
        int MaxPlayers { get; }

        /// <summary>
        ///     Raised when the <see cref="SessionStatus" /> of a <see cref="IPlayerSession" /> is changed.
        /// </summary>
        event EventHandler<SessionStatusEventArgs> PlayerStatusChanged;

        /// <summary>
        ///     Initializes the manager.
        /// </summary>
        /// <param name="baseServer">The server that instantiated this manager.</param>
        /// <param name="maxPlayers">Maximum number of players that can connect to this server at one time.</param>
        void Initialize(int maxPlayers);

        /// <summary>
        ///     Returns the client session of the networkId.
        /// </summary>
        /// <returns></returns>
        IPlayerSession GetSessionById(NetSessionId index);

        IPlayerSession GetSessionByChannel(INetChannel channel);

        bool TryGetSessionById(NetSessionId sessionId, out IPlayerSession session);

        /// <summary>
        ///     Checks to see if a PlayerIndex is a valid session.
        /// </summary>
        bool ValidSessionId(NetSessionId index);

        IPlayerData GetPlayerData(NetSessionId sessionId);
        bool TryGetPlayerData(NetSessionId sessionId, out IPlayerData data);
        bool HasPlayerData(NetSessionId sessionId);

        void SendJoinGameToAll();
        void SendJoinLobbyToAll();

        void DetachAll();
        List<IPlayerSession> GetPlayersInLobby();
        List<IPlayerSession> GetPlayersInRange(GridLocalCoordinates worldPos, int range);
        List<IPlayerSession> GetAllPlayers();
        List<PlayerState> GetPlayerStates();
    }
}
