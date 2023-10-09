using System;
using System.Collections.Generic;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Robust.Shared.Player;

/// <summary>
/// Common info between client and server sessions.
/// </summary>
public interface ICommonSession
{
    /// <summary>
    /// Status of the session.
    /// </summary>
    SessionStatus Status { get; }

    /// <summary>
    /// Entity UID that this session is represented by in the world, if any.
    /// </summary>
    EntityUid? AttachedEntity { get; }

    /// <summary>
    /// The UID of this session.
    /// </summary>
    NetUserId UserId { get; }

    /// <summary>
    /// Current name of this player.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Current connection latency of this session from the server to their client.
    /// </summary>
    short Ping { get; internal set; }

    /// <summary>
    /// The current network channel for this session.
    /// </summary>
    /// <remarks>
    /// On the Server every player has a network channel,
    /// on the Client only the LocalPlayer has a network channel, and that channel points to the server.
    /// </remarks>
    INetChannel? Channel { get; }

    LoginType AuthType { get; }

    /// <summary>
    /// List of "eyes" to use for PVS range checks.
    /// </summary>
    HashSet<EntityUid> ViewSubscriptions { get; }

    DateTime ConnectedTime { get; set; }

    /// <summary>
    /// Session state, for sending player lists to clients.
    /// </summary>
    SessionState State { get; }

    /// <summary>
    /// Class for storing arbitrary session-specific data that is not lost upon reconnect.
    /// </summary>
    SessionData Data { get; }

    [Obsolete("Use IPlayerManager")]
    void JoinGame() => IoCManager.Resolve<ISharedPlayerManager>().SetStatus(this, SessionStatus.InGame);

    [Obsolete("use the Channel field instead.")]
    INetChannel ConnectedClient => Channel!;
}