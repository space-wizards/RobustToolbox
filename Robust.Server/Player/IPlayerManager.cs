using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Robust.Server.Player
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

        bool TryGetSessionByUsername(string username, [NotNullWhen(true)] out IPlayerSession? session);

        /// <summary>
        ///     Returns the client session of the networkId.
        /// </summary>
        /// <returns></returns>
        IPlayerSession GetSessionByUserId(NetUserId index);

        IPlayerSession GetSessionByChannel(INetChannel channel);

        bool TryGetSessionByChannel(INetChannel channel, [NotNullWhen(true)] out IPlayerSession? session);

        bool TryGetSessionById(NetUserId userId, [NotNullWhen(true)] out IPlayerSession? session);

        /// <summary>
        ///     Checks to see if a PlayerIndex is a valid session.
        /// </summary>
        bool ValidSessionId(NetUserId index);

        IPlayerData GetPlayerData(NetUserId userId);
        bool TryGetPlayerData(NetUserId userId, [NotNullWhen(true)] out IPlayerData? data);
        bool TryGetPlayerDataByUsername(string userName, [NotNullWhen(true)] out IPlayerData? data);
        bool HasPlayerData(NetUserId userId);

        /// <summary>
        ///     Tries to get the user ID of the user with the specified username.
        /// </summary>
        /// <remarks>
        ///     This only works if this user has already connected once before during this server run.
        ///     It does still work if the user has since disconnected.
        /// </remarks>
        bool TryGetUserId(string userName, out NetUserId userId);

        IEnumerable<IPlayerData> GetAllPlayerData();

        void DetachAll();
        List<IPlayerSession> GetPlayersInRange(MapCoordinates worldPos, int range);
        List<IPlayerSession> GetPlayersInRange(EntityCoordinates worldPos, int range);
        List<IPlayerSession> GetPlayersBy(Func<IPlayerSession, bool> predicate);
        List<IPlayerSession> GetAllPlayers();
        List<PlayerState>? GetPlayerStates(GameTick fromTick);
    }
}
