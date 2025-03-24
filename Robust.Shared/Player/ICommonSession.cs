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
    /// Current connection latency of this session. If <see cref="Channel"/> is not null this simply returns
    /// <see cref="INetChannel.Ping"/>. This is not currently usable by client-side code that wants to try access ping
    /// information of other players.
    /// </summary>
    short Ping { get; }
    // TODO PlayerManager ping networking.

    /// <summary>
    /// The current network channel for this session.
    /// </summary>
    /// <remarks>
    /// On the Server every player has a network channel,
    /// on the Client only the LocalPlayer has a network channel, and that channel points to the server.
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

    /// <summary>
    /// If true, this indicates that this is a client-side session, and should be ignored when applying a server's
    /// game state.
    /// </summary>
    bool ClientSide { get; set; }
}

internal interface ICommonSessionInternal : ICommonSession
{
    public void SetStatus(SessionStatus status);
    public void SetAttachedEntity(EntityUid? uid);
    public void SetPing(short ping);
    public void SetName(string name);
    void SetChannel(INetChannel channel);
}
