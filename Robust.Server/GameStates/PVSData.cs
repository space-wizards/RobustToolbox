
using System.Collections.Generic;
using Robust.Shared.Collections;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Robust.Server.GameStates;

/// <summary>
///     Class used to store per-session data in order to avoid having to lock dictionaries.
/// </summary>
internal sealed class SessionPvsData
{
    /// <summary>
    /// All <see cref="EntityUid"/>s that this session saw during the last <see cref="DirtyBufferSize"/> ticks.
    /// </summary>
    public readonly OverflowDictionary<GameTick, Dictionary<NetEntity, PvsEntityVisibility>> SentEntities = new(PvsSystem.DirtyBufferSize);

    /// <summary>
    ///     The most recently acked entities
    /// </summary>
    public (GameTick Tick, Dictionary<NetEntity, PvsEntityVisibility> Data)? LastAcked;

    public readonly Dictionary<NetEntity, EntityData> EntityData = new();

    /// <summary>
    ///     <see cref="SentEntities"/> overflow in case a player's last ack is more than <see cref="DirtyBufferSize"/> ticks behind the current tick.
    /// </summary>
    public (GameTick Tick, Dictionary<NetEntity, PvsEntityVisibility> SentEnts)? Overflow;

    /// <summary>
    ///     If true, the client has explicitly requested a full state. Unlike the first state, we will send them
    ///     all data, not just data that cannot be implicitly inferred from entity prototypes.
    /// </summary>
    public bool RequestedFull = false;

    /// <summary>
    ///     The tick of the most recently received client Ack. Will be used to update <see cref="LastAcked"/>
    /// </summary>
    /// <remarks>
    ///     As the server delays processing acks, this might not currently be the same as <see cref="LastAcked"/>
    /// </remarks>
    public GameTick LastReceivedAck;
}

// TODO PVS turn this into a struct when this gets stored in a list/array instead of a dictionary
internal sealed class EntityData
{
    public PvsEntityVisibility ToSend;

    public PvsEntityVisibility[] PreviouslySent = new PvsEntityVisibility[PvsSystem.DirtyBufferSize];

    public (GameTick, PvsEntityVisibility) LastAcked;

    public (GameTick, PvsEntityVisibility) PreviouslySentOverflow;

    /// <summary>
    ///     Tick at which an entity last left a player's PVS view.
    /// </summary>
    public GameTick LastLeftView;

    /// <summary>
    ///     Stores the last tick at which a given entity was acked by a player. Used to avoid re-sending the whole entity
    ///     state when an item re-enters PVS.
    /// </summary>
    public GameTick LastSeenAt;
}
