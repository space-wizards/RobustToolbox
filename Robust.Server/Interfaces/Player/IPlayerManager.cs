using System;
using System.Collections.Generic;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Robust.Server.Interfaces.Player
{
    /// <summary>
    ///     Manages each players session when connected to the server.
    /// </summary>
    public interface IPlayerManager
    {
        /// <summary>
        ///     Number of players currently connected to this server.
        ///     Fetching this is thread safe.
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

        IEnumerable<IPlayerData> GetAllPlayerData();

        void DetachAll();
        List<IPlayerSession> GetPlayersInRange(GridCoordinates worldPos, int range);
        List<IPlayerSession> GetPlayersBy(Func<IPlayerSession, bool> predicate);
        List<IPlayerSession> GetAllPlayers();
        List<PlayerState> GetPlayerStates(GameTick fromTick);
    }
}
