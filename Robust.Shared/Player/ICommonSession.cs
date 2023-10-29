using System;
using System.Collections.Generic;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
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
    string Name { get; }

    /// <summary>
    /// Current connection latency of this session from the server to their client.
    /// </summary>
    short Ping { get; }

    /// <summary>
    /// The current network channel for this session.
    /// </summary>
    /// <remarks>
    /// On the Server every player has a network channel,
    /// on the Client only the LocalPlayer has a network channel, and that channel points to the server.
    /// Unless you know what you are doing, you shouldn't be modifying this directly.
    /// </remarks>
    INetChannel Channel { get; set; }

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

    [Obsolete("Just use the Channel field instead.")]
    INetChannel ConnectedClient => Channel;

    /// <summary>
    /// If true, this indicates that this is a client-side session, and should be ignored when applying a server's
    /// game state.
    /// </summary>
    bool ClientSide { get; set; }
}
