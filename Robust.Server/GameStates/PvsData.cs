using System;
using System.Collections.Generic;
using Robust.Shared.Collections;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

/// <summary>
/// Class for storing session specific PVS data.
/// </summary>
internal sealed class PvsSession(ICommonSession session)
{
    public readonly ICommonSession Session = session;
    public INetChannel Channel => Session.Channel;

    /// <summary>
    /// All <see cref="EntityUid"/>s that this session saw during the last <see cref="PvsSystem.DirtyBufferSize"/> ticks.
    /// </summary>
    public readonly OverflowDictionary<GameTick, List<PvsData>> PreviouslySent = new(PvsSystem.DirtyBufferSize);

    /// <summary>
    /// Dictionary containing data about all entities that this client has ever seen.
    /// </summary>
    public readonly Dictionary<NetEntity, PvsData> Entities = new();

    /// <summary>
    /// <see cref="PreviouslySent"/> overflow in case a player's last ack is more than
    /// <see cref="PvsSystem.DirtyBufferSize"/> ticks behind the current tick.
    /// </summary>
    public (GameTick Tick, List<PvsData> SentEnts)? Overflow;

    /// <summary>
    /// The client's current visibility mask.
    /// </summary>
    public int VisMask;

    /// <summary>
    /// The list that is currently being prepared for sending.
    /// </summary>
    public List<PvsData>? ToSend;

    /// <summary>
    /// The <see cref="ToSend"/> list from the previous tick. Also caches the current tick that the PVS leave message
    /// should belong to, in case the processing is ever run asynchronously with normal system/game ticking.
    /// </summary>
    public (GameTick ToTick, List<PvsData> PreviouslySent)? LastSent;

    /// <summary>
    /// Visible chunks, sorted by proximity to the clients's viewers;
    /// </summary>
    public readonly List<(PvsChunk Chunk, float ChebyshevDistance)> Chunks = new();

    /// <summary>
    /// Squared distance ta all of the visible chunks.
    /// </summary>
    public readonly List<float> ChunkDistanceSq = new();

    /// <summary>
    /// The client's current eyes/viewers.
    /// </summary>
    public Entity<TransformComponent, EyeComponent?>[] Viewers
        = Array.Empty<Entity<TransformComponent, EyeComponent?>>();

    /// <summary>
    /// If true, the client has explicitly requested a full state. Unlike the first state, we will send them all data,
    /// not just data that cannot be implicitly inferred from entity prototypes.
    /// </summary>
    public bool RequestedFull = false;

    /// <summary>
    /// List of entity states to send to the client.
    /// </summary>
    public readonly List<EntityState> States = new();

    /// <summary>
    /// Information about the current number of entities that are being sent to the player this tick. Used to enforce
    /// pvs budgets.
    /// </summary>
    public PvsBudget Budget;

    /// <summary>
    /// The tick of the last acknowledged game state.
    /// </summary>
    public GameTick LastReceivedAck;

    /// <summary>
    /// Start tick for the time window of data that has to be sent to this player.
    /// </summary>
    public GameTick FromTick;

    // TODO PVS support this properly. I.e., add a command, and remove from _seenAllEnts
    public bool DisableCulling;

    /// <summary>
    /// List of entities that have left the player's view this tick.
    /// </summary>
    public readonly List<NetEntity> LeftView = new();

    public readonly List<SessionState> PlayerStates = new();
    public uint LastMessage;
    public uint LastInput;

    /// <summary>
    /// The game state for this tick,
    /// </summary>
    public GameState? State;

    /// <summary>
    /// Clears all stored game state data. This should only be used after the game state has been serialized.
    /// </summary>
    public void ClearState()
    {
        PlayerStates.Clear();
        Chunks.Clear();
        States.Clear();
        State = null;
    }
}

/// <summary>
/// Class for storing session-specific information about when an entity was last sent to a player.
/// </summary>
internal sealed class PvsData(NetEntity entity) : IEquatable<PvsData>
{
    public readonly NetEntity NetEntity = entity;

    /// <summary>
    /// Tick at which this entity was last sent to a player.
    /// </summary>
    public GameTick LastSeen;

    /// <summary>
    /// Tick at which an entity last left a player's PVS view.
    /// </summary>
    public GameTick LastLeftView;

    /// <summary>
    /// Stores the last tick at which a given entity was acked by a player. Used to avoid re-sending the whole entity
    /// state when an item re-enters PVS. This is only the same as the player's last acked tick if the entity was
    /// present in that state.
    /// </summary>
    public GameTick EntityLastAcked;

    public bool Equals(PvsData? other)
    {
        DebugTools.Assert((NetEntity != other?.NetEntity) || ReferenceEquals(this, other));
        return NetEntity == other?.NetEntity;
    }

    public override int GetHashCode()
    {
        return NetEntity.GetHashCode();
    }
}

/// <summary>
/// Struct for storing information about the current number of entities that are being sent to the player this tick.
/// Used to enforce pvs budgets.
internal struct PvsBudget
{
    public int NewLimit;
    public int EnterLimit;
    public int DirtyCount;
    public int EnterCount;
    public int NewCount;
}
