using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Shared.Collections;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

/// <summary>
/// Class for storing session specific PVS data.
/// </summary>
internal sealed class PvsSession(ICommonSession session, ResizableMemoryRegion<PvsData> memoryRegion)
{
#if DEBUG
    public HashSet<NetEntity> ToSendSet = new();
#endif

    public readonly ICommonSession Session = session;

    public readonly ResizableMemoryRegion<PvsData> DataMemory = memoryRegion;

    public INetChannel Channel => Session.Channel;

    /// <summary>
    /// All entities that this session saw during the last <see cref="PvsSystem.DirtyBufferSize"/> ticks.
    /// </summary>
    public readonly OverflowDictionary<GameTick, List<PvsIndex>> PreviouslySent = new(PvsSystem.DirtyBufferSize);

    /// <summary>
    /// <see cref="PreviouslySent"/> overflow in case a player's last ack is more than
    /// <see cref="PvsSystem.DirtyBufferSize"/> ticks behind the current tick.
    /// </summary>
    public (GameTick Tick, List<PvsIndex> SentEnts)? Overflow;

    /// <summary>
    /// The client's current visibility mask.
    /// </summary>
    public int VisMask;

    /// <summary>
    /// The list that is currently being prepared for sending.
    /// </summary>
    public List<PvsIndex>? ToSend;

    /// <summary>
    /// The <see cref="ToSend"/> list from the previous tick. Also caches the current tick that the PVS leave message
    /// should belong to, in case the processing is ever run asynchronously with normal system/game ticking.
    /// </summary>
    public (GameTick ToTick, List<PvsIndex> PreviouslySent)? LastSent;

    /// <summary>
    /// Visible chunks, sorted by proximity to the client's viewers.
    /// </summary>
    public readonly List<(PvsChunk Chunk, float ChebyshevDistance)> Chunks = new();

    /// <summary>
    /// Unsorted set of visible chunks. Used to construct the <see cref="Chunks"/> list.
    /// </summary>
    public readonly HashSet<PvsChunk> ChunkSet = new();

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
    /// The serialized <see cref="State"/> object.
    /// </summary>
    public MemoryStream? StateStream;

    /// <summary>
    /// Whether we should force reliable sending of the <see cref="MsgState"/>.
    /// </summary>
    public bool ForceSendReliably { get; set; }

    /// <summary>
    /// Clears all stored game state data. This should only be used after the game state has been serialized.
    /// </summary>
    public void ClearState()
    {
        PlayerStates.Clear();
        Chunks.Clear();
        ChunkSet.Clear();
        States.Clear();
        State = null;
    }
}

/// <summary>
/// Class for storing session-specific information about when an entity was last sent to a player.
/// </summary>
/// <remarks>
/// Size is padded to 16 bytes so
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 16)]
internal struct PvsData
{
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
}

/// <summary>
/// Specialized struct with the same size as <see cref="PvsData"/> that is used to store metadata in the pinned PVsData array
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 16)]
internal struct PvsMetadata
{
    /// <summary>
    /// Tick at which this entity was last sent to a player.
    /// </summary>
    public NetEntity NetEntity;

    public GameTick LastModifiedTick;

    // TODO PVS maybe store as int?
    // Theres extra space anyways, and the mask checks always need to convert to an int first, so it'd probably be faster too.
    public ushort VisMask;
    public EntityLifeStage LifeStage;
#if DEBUG
    // This struct is padded to a size of 16 so it's aligned to cache boundaries nicely.
    // We have this extra space that isn't being used,
    // so I'm opting to use them to make debugging the free list easier.
    // "Marker" overlaps with the field used by the free list (which occupies the unused memory of PvsMetadata).
    // So we set it to a bogus value and BAM! Errors are obvious!
    private byte Pad0;
    public uint Marker;
#endif

    [Conditional("DEBUG")]
    public void Validate(MetaDataComponent comp)
    {
        DebugTools.AssertEqual(NetEntity, comp.NetEntity);
        DebugTools.AssertEqual(VisMask, comp.VisibilityMask);
        DebugTools.AssertEqual(LifeStage, comp.EntityLifeStage);
        DebugTools.Assert(LastModifiedTick == comp.EntityLastModifiedTick || LastModifiedTick.Value == 0);
    }
}

[StructLayout(LayoutKind.Sequential, Size = 16)]
internal struct PvsMetadataFreeLink
{
#if DEBUG
    static unsafe PvsMetadataFreeLink()
    {
        DebugTools.Assert(sizeof(PvsMetadataFreeLink) == sizeof(PvsMetadata));
    }
#endif

    public int Pad0;
    public int Pad1;
    public int Pad2;
    // We offset the NextFree to be at the end of the struct.
    // This is so that it overlaps with the debug Marker field of PvsMetadata instead of real data.
    public PvsIndex NextFree;
}

/// <summary>
/// Struct for storing information about the current number of entities that are being sent to the player this tick.
/// Used to enforce pvs budgets.
/// </summary>
internal struct PvsBudget
{
    public int NewLimit;
    public int EnterLimit;
    public int DirtyCount;
    public int EnterCount;
    public int NewCount;
}
