using System;
using System.Collections.Generic;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Robust.Shared.Player;

/// <summary>
///     Common info between client and server sessions.
/// </summary>
/// <seealso cref="ISharedPlayerManager"/>
[NotContentImplementable]
public interface ICommonSession
{
    /// <summary>
    ///     Status of the session, dictating the connection state.
    /// </summary>
    SessionStatus Status { get; }

    /// <summary>
    ///     Entity UID that this session is represented by in the world, if any. Any attached entity will have
    ///     <see cref="ActorComponent"/>.
    /// </summary>
    /// <seealso cref="ActorSystem"/>
    EntityUid? AttachedEntity { get; }

    /// <summary>
    ///     The unique user ID of this session.
    /// </summary>
    /// <remarks>
    ///     If this user's <see cref="AuthType"/> is <see cref="LoginType.LoggedIn"/> or
    ///     <see cref="LoginType.GuestAssigned"/> (<see cref="LoginTypeExt.HasStaticUserId"/>),
    ///     their user id is globally unique as described on
    ///     <see cref="NetUserId"/>.
    /// </remarks>
    NetUserId UserId { get; }

    /// <summary>
    ///     Current name of this player.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Current connection latency of this session. If <see cref="Channel"/> is not null this simply returns
    ///     <see cref="INetChannel.Ping"/>. This is not currently usable by client-side code that wants to try access
    ///     ping information of other players.
    /// </summary>
    short Ping { get; }
    // TODO PlayerManager ping networking.

    /// <summary>
    ///     The current network channel for this session, if one exists.
    /// </summary>
    /// <remarks>
    ///     On the server every player has a network channel,
    ///     and on the client only the LocalPlayer has a network channel, and that channel points to the server.
    ///     Sessions without channels will have null here.
    /// </remarks>
    INetChannel Channel { get; set; }

    /// <summary>
    ///     How this session logged in, which dictates the uniqueness of <see cref="UserId"/>.
    /// </summary>
    LoginType AuthType { get; }

    /// <summary>
    ///     List of "eyes" to use for PVS range checks.
    /// </summary>
    HashSet<EntityUid> ViewSubscriptions { get; }

    /// <summary>
    ///     The last time this session connected.
    /// </summary>
    DateTime ConnectedTime { get; set; }

    /// <summary>
    ///     Session state, for sending player lists to clients.
    /// </summary>
    SessionState State { get; }

    /// <summary>
    ///     Class for storing arbitrary session-specific data that is not lost upon reconnect.
    /// </summary>
    SessionData Data { get; }

    /// <summary>
    ///     If true, this indicates that this is a client-side session, and should be ignored when applying a server's
    ///     game state.
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
