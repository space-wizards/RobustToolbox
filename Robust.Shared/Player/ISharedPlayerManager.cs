using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Player;

public interface ISharedPlayerManager
{
    /// <summary>
    /// list of connected sessions.
    /// </summary>
    ICommonSession[] Sessions { get; }

    /// <summary>
    /// Sessions with a remote endpoint. On the server, this is equivalent to <see cref="Sessions"/>. On the client,
    /// this will only ever contain <see cref="LocalSession"/>
    /// </summary>
    ICommonSession[] NetworkedSessions { get; }

    /// <summary>
    /// Dictionary mapping connected users to their sessions.
    /// </summary>
    IReadOnlyDictionary<NetUserId, ICommonSession> SessionsDict { get; }

    /// <summary>
    ///     Number of players currently connected to this server.
    /// </summary>
    int PlayerCount { get; }

    /// <summary>
    ///     Maximum number of players that can connect to this server at one time.
    /// </summary>
    int MaxPlayers { get; }

    /// <summary>
    /// Initializes the manager.
    /// </summary>
    /// <param name="maxPlayers">Maximum number of players that can connect to this server at one time. Does nothing
    /// on the client.</param>
    void Initialize(int maxPlayers);

    void Startup();
    void Shutdown();

    /// <summary>
    /// Indicates that some session's networked data has changed. This will cause an updated player list to be sent to
    /// all players.
    /// </summary>
    void Dirty();

    /// <summary>
    /// The session of the local player. This will be null on the server.
    /// </summary>
    [ViewVariables] ICommonSession? LocalSession { get; }

    /// <summary>
    /// The user Id of the local player. This will be null on the server.
    /// </summary>
    [ViewVariables] NetUserId? LocalUser { get; }

    /// <summary>
    /// The entity currently controlled by the local player. This will be null on the server.
    /// </summary>
    [ViewVariables] EntityUid? LocalEntity { get; }

    /// <summary>
    /// This gets invoked when a session's <see cref="ICommonSession.Status"/> changes.
    /// </summary>
    event EventHandler<SessionStatusEventArgs>? PlayerStatusChanged;

    /// <summary>
    /// Attempts to resolve a username into a <see cref="NetUserId"/>.
    /// </summary>
    bool TryGetUserId(string userName, out NetUserId userId);

    /// <summary>
    /// Attempts to get the session that is currently attached to a given entity.
    /// </summary>
    bool TryGetSessionByEntity(EntityUid uid, [NotNullWhen(true)] out ICommonSession? session);

    /// <summary>
    /// Attempts to get the session with the given <see cref="NetUserId"/>.
    /// </summary>
    bool TryGetSessionById([NotNullWhen(true)] NetUserId? user, [NotNullWhen(true)] out ICommonSession? session);

    /// <summary>
    /// Attempts to get the session with the given <see cref="ICommonSession.Name"/>.
    /// </summary>
    bool TryGetSessionByUsername(string username, [NotNullWhen(true)] out ICommonSession? session);

    /// <summary>
    /// Attempts to get the session that corresponds to the given channel.
    /// </summary>
    bool TryGetSessionByChannel(INetChannel channel, [NotNullWhen(true)] out ICommonSession? session);

    ICommonSession GetSessionByChannel(INetChannel channel) => GetSessionById(channel.UserId);

    ICommonSession GetSessionById(NetUserId user);

    /// <summary>
    /// Check if the given user id has an active session.
    /// </summary>
    bool ValidSessionId(NetUserId user) => TryGetSessionById(user, out _);

    SessionData GetPlayerData(NetUserId userId);
    bool TryGetPlayerData(NetUserId userId, [NotNullWhen(true)] out SessionData? data);
    bool TryGetPlayerDataByUsername(string userName, [NotNullWhen(true)] out SessionData? data);
    bool HasPlayerData(NetUserId userId);

    IEnumerable<SessionData> GetAllPlayerData();
    void GetPlayerStates(GameTick fromTick, List<SessionState> states);
    void UpdateState(ICommonSession commonSession);

    void RemoveSession(ICommonSession session, bool removeData = false);
    void RemoveSession(NetUserId user, bool removeData = false);

    ICommonSession CreateAndAddSession(INetChannel channel);

    ICommonSession CreateAndAddSession(NetUserId user, string name);

    /// <summary>
    /// Sets a session's attached entity, optionally kicking any sessions already attached to it.
    /// </summary>
    /// <param name="session">The player whose attached entity should get updated</param>
    /// <param name="entity">The entity to attach the player to, if any.</param>
    /// <param name="force">Whether to kick any existing players that are already attached to the entity</param>
    /// <param name="kicked">The player that was forcefully kicked, if any.</param>
    /// <returns>Whether the attach succeeded, or not.</returns>
    bool SetAttachedEntity(
        [NotNullWhen(true)] ICommonSession? session,
        EntityUid? entity,
        out ICommonSession? kicked,
        bool force = false);

    /// <summary>
    /// Sets a session's attached entity, optionally kicking any sessions already attached to it.
    /// </summary>
    /// <param name="session">The player whose attached entity should get updated</param>
    /// <param name="entity">The entity to attach the player to, if any.</param>
    /// <param name="force">Whether to kick any existing players that are already attached to the entity</param>
    /// <returns>Whether the attach succeeded, or not.</returns>
    bool SetAttachedEntity([NotNullWhen(true)] ICommonSession? session, EntityUid? entity, bool force = false)
        => SetAttachedEntity(session, entity, out _, force);

    /// <summary>
    /// Updates a session's <see cref="ICommonSession.Status"/>
    /// </summary>
    void SetStatus(ICommonSession session, SessionStatus status);

    /// <summary>
    /// Updates a session's <see cref="ICommonSession.Ping"/>
    /// </summary>
    void SetPing(ICommonSession session, short ping);

    /// <summary>
    /// Updates a session's <see cref="ICommonSession.Name"/>
    /// </summary>
    public void SetName(ICommonSession session, string name);

    /// <summary>
    /// Set the session's status to <see cref="SessionStatus.InGame"/>.
    /// </summary>
    void JoinGame(ICommonSession session);
}
