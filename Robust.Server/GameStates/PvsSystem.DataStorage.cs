using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

// This partial class handles the PvsData memory. This array stores information about when each entity was last sent to
// each player. This is somewhat faster than using a per-player Dictionary<EntityUid, PvsData>, though it can be less
// memory efficient.
internal sealed partial class PvsSystem
{
    // This is used for asserts.
    private HashSet<PvsIndex> _assignedEnts = new();

    /// <summary>
    /// Recently returned indexes from deleted entities. These get moved to <see cref="_pendingReturns"/> before
    /// moving back into the free list.
    /// </summary>
    private List<PvsIndex> _incomingReturns = new();

    /// <summary>
    /// Recently returned pointers from deleted entities. These will get returned to the free list
    /// after a minimum amount of time has passed, to ensure that processing late game-state ack messages doesn't
    /// write data to deleted entities.
    /// </summary>
    private List<PvsIndex> _pendingReturns = new();

    /// <summary>
    /// Tick at which the <see cref="_pendingReturns"/> were last processed.
    /// </summary>
    private GameTick _lastReturn = GameTick.Zero;

    /// <summary>
    /// Memory region to store <see cref="PvsMetadata"/> instances and the free list.
    /// </summary>
    /// <remarks>
    /// Unused elements form a linked list out of <see cref="PvsMetadataFreeLink"/> elements.
    /// </remarks>
    private ResizableMemoryRegion<PvsMetadata> _metadataMemory = default!;

    /// <summary>
    /// The head of the PVS data free list. This is the first element that will be used if a new one is needed.
    /// </summary>
    /// <remarks>
    /// If the value is <see cref="PvsIndex.Invalid"/>,
    /// there are no more free elements and the next allocation must expand the memory.
    /// </remarks>
    private PvsIndex _dataFreeListHead;

    private WaitHandle? _deletionTask;

    /// <summary>
    /// Expand the size of <see cref="_metadataMemory"/> (and all session data stores) one iteration.
    /// </summary>
    /// <remarks>
    /// This ensures that we have at least one free list slot.
    /// </remarks>
    private void ExpandEntityCapacity()
    {
        var initial = _metadataMemory.CurrentSize;

        var entityGrowth = _configManager.GetCVar(CVars.NetPvsEntityGrowth);
        var newSize = initial + (entityGrowth <= 0 ? initial : entityGrowth);
        newSize = Math.Min(newSize, _metadataMemory.MaxSize);
        if (newSize == initial)
            throw new InvalidOperationException("Out of PVS entity capacity! Increase net.pvs_entity_max!");

        Log.Debug($"Growing PvsData memory from {initial} -> {newSize} entities");

        _metadataMemory.Expand(newSize);
        foreach (var playerSession in PlayerData.Values)
        {
            playerSession.DataMemory.Expand(newSize);
        }

        var newSlots = _metadataMemory.GetSpan<PvsMetadataFreeLink>()[initial..];
        InitializeFreeList(newSlots, initial, ref _dataFreeListHead);
    }

    /// <summary>
    /// Initialize <see cref="_metadataMemory"/> and the free list.
    /// </summary>
    private void InitializePvsArray()
    {
        var initialCount = _configManager.GetCVar(CVars.NetPvsEntityInitial);
        var maxCount = _configManager.GetCVar(CVars.NetPvsEntityMax);

        if (initialCount <= 0 || maxCount <= 0)
            throw new InvalidOperationException("net.pvs_entity_initial and net.pvs_entity_max must be positive");

        _metadataMemory = new ResizableMemoryRegion<PvsMetadata>(maxCount, initialCount);

        ResetDataMemory();
    }

    /// <summary>
    /// Initialize a section of the free list.
    /// </summary>
    /// <param name="memory">The section of the free list to initialize.</param>
    /// <param name="baseOffset">What offset in the total PVS data this section starts at.</param>
    /// <param name="head">The current head storage of the free list to update.</param>
    private static void InitializeFreeList(Span<PvsMetadataFreeLink> memory, int baseOffset, ref PvsIndex head)
    {
        for (var i = 0; i < memory.Length; i++)
        {
            memory[i].NextFree = new PvsIndex(baseOffset + i + 1);
        }

        memory[^1].NextFree = head;
        head = new PvsIndex(baseOffset);
    }

    /// <summary>
    /// Clear all PVS data. After this function is called,
    /// <see cref="ResetDataMemory"/> must be called if the system isn't being shut down.
    /// </summary>
    private void ClearPvsData()
    {
        _leaveTask?.WaitOne();
        _leaveTask = null;

        _deletionTask?.WaitOne();
        _deletionTask = null;

        _incomingReturns.Clear();
        _pendingReturns.Clear();
        _deletionJob.ToClear.Clear();
        _assignedEnts.Clear();

        // Remove all pointers stored in any player's PVS send-histories. Required to avoid accidentally writing to
        // invalid bits of memory while processing late game-state acks. This also forces all players to receive a full
        // game state, in lieu of sending the required PVS leave messages.
        foreach (var session in PlayerData.Values)
        {
            session.DataMemory.Clear();
            ForceFullState(session);
        }

        _metadataMemory.Clear();
    }

    /// <summary>
    /// Re-initialize the memory in <see cref="_metadataMemory"/> after it was fully cleared on reset.
    /// </summary>
    private void ResetDataMemory()
    {
        _dataFreeListHead = PvsIndex.Invalid;
        InitializeFreeList(_metadataMemory.GetSpan<PvsMetadataFreeLink>(), 0, ref _dataFreeListHead);
    }

    /// <summary>
    /// Shrink <see cref="_metadataMemory"/> (and all sessions) back down to initial entity size after clear.
    /// </summary>
    private void ShrinkDataMemory()
    {
        DebugTools.Assert(EntityManager.EntityCount == 0);

        var initialCount = _configManager.GetCVar(CVars.NetPvsEntityInitial);

        if (initialCount != _metadataMemory.CurrentSize)
        {
            Log.Debug($"Shrinking PVS data from {_metadataMemory.CurrentSize} -> {initialCount} entities");

            _metadataMemory.Shrink(initialCount);
            foreach (var player in PlayerData.Values)
            {
                player.DataMemory.Shrink(initialCount);
            }
        }
    }

    /// <summary>
    /// This method shuffles the entity free list. This is used to avoid accidental / unrealistic cache locality
    /// in benchmarks.
    /// </summary>
    internal void ShufflePointers(int seed)
    {
        throw new NotImplementedException();
        /*List<IntPtr> ptrs = new(_pointerPool);
        _pointerPool.Clear();

        var rng = new Random(seed);
        var n = ptrs.Count;
        while (n > 0)
        {
            var k = rng.Next(n);
            _pointerPool.Push(ptrs[k]);
            ptrs[k] = ptrs[^1];
            ptrs.RemoveAt(--n);
        }*/
    }

    /// <summary>
    /// Clear all of this sessions' PvsData for all entities. This effectively means that PVS will act as if the player
    /// had never been sent information about any entity. Used when returning the player's index offset to the pool.
    /// </summary>
    private void ClearPlayerPvsData(PvsSession session)
    {
        session.DataMemory.Clear();
    }

    /// <summary>
    /// Clear all of this entity' PvsData entries. This effectively means that PVS will act as if no player
    /// had never been sent information about this entity. Used when returning the entity's index back to the free list.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClearEntityPvsData(PvsIndex index)
    {
        foreach (var playerData in PlayerData.Values)
        {
            ref var entry = ref playerData.DataMemory.GetRef(index.Index);
            entry = default;
        }
    }

    /// <summary>
    /// Get the NetEntity associated with a given <see cref="PvsIndex"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private NetEntity IndexToNetEntity(PvsIndex index)
    {
        DebugTools.Assert(_assignedEnts.Contains(index));
        return _metadataMemory.GetRef(index.Index).NetEntity;
    }

    /// <summary>
    /// Create a new <see cref="ResizableMemoryRegion{T}"/> suitable for assigning to a new <see cref="PvsSession"/>.
    /// </summary>
    private ResizableMemoryRegion<PvsData> CreateSessionDataMemory()
    {
        return new ResizableMemoryRegion<PvsData>(_metadataMemory.MaxSize, _metadataMemory.CurrentSize);
    }

    private static void FreeSessionDataMemory(PvsSession session)
    {
        session.DataMemory.Dispose();
    }

    private void OnEntityAdded(Entity<MetaDataComponent> entity)
    {
        AssignEntityPointer(entity.Comp);
    }

    /// <summary>
    /// Retrieve a free entity index and assign it to an entity.
    /// </summary>
    private void AssignEntityPointer(MetaDataComponent meta)
    {
        DebugTools.Assert(meta.PvsData == PvsIndex.Invalid);
        if (_dataFreeListHead == PvsIndex.Invalid)
        {
            ExpandEntityCapacity();
            DebugTools.Assert(_dataFreeListHead != PvsIndex.Invalid);
        }

        var index = _dataFreeListHead;
        DebugTools.Assert(_assignedEnts.Add(index));
        ref var metadata = ref _metadataMemory.GetRef(index.Index);
        ref var freeLink = ref Unsafe.As<PvsMetadata, PvsMetadataFreeLink>(ref metadata);
        _dataFreeListHead = freeLink.NextFree;

        DebugTools.AssertNotEqual(meta.NetEntity, NetEntity.Invalid);

        meta.PvsData = index;
        metadata.NetEntity = meta.NetEntity;
        metadata.LastModifiedTick = meta.LastModifiedTick;
        metadata.VisMask = meta.VisibilityMask;
        metadata.LifeStage = meta.EntityLifeStage;
#if DEBUG
        metadata.Marker = uint.MaxValue;
#endif
    }

    /// <summary>
    /// Return an entity's index in the data array back to the free list of available indices.
    /// </summary>
    private void OnEntityDeleted(Entity<MetaDataComponent> entity)
    {
        var ptr = entity.Comp.PvsData;
        entity.Comp.PvsData = PvsIndex.Invalid;

        if (ptr == PvsIndex.Invalid)
            return;

        _incomingReturns.Add(ptr);
    }

    /// <summary>
    /// Immediately return all data indexes back to the pool after flushing all entities.
    /// </summary>
    private void AfterEntityFlush()
    {
        if (EntityManager.EntityCount > 0)
            throw new Exception("Cannot reset PVS data without first deleting all entities.");

        ClearPvsData();
        ShrinkDataMemory();
        ResetDataMemory();
    }

    /// <summary>
    /// This update method periodically returns entity indices back to the pool, once we are sure no old
    /// game state acks will use indices to that entity.
    /// </summary>
    private void ProcessDeletions()
    {
        var curTick = _gameTiming.CurTick;

        if (curTick < _lastReturn + (uint)ForceAckThreshold + 1)
            return;

        if (curTick < _lastReturn)
            throw new InvalidOperationException($"Time travel is not supported");

        _leaveTask?.WaitOne();
        _leaveTask = null;

        _deletionTask?.WaitOne();
        _deletionTask = null;

        _lastReturn = curTick;

        foreach (var index in CollectionsMarshal.AsSpan(_deletionJob.ToClear))
        {
            ReturnEntity(index);
        }
        _deletionJob.ToClear.Clear();

        // Cycle lists.
        (_deletionJob.ToClear, _pendingReturns, _incomingReturns) = (_pendingReturns, _incomingReturns, _deletionJob.ToClear);

        if (_deletionJob.ToClear.Count == 0)
            return;

        #if DEBUG
        foreach (var index in CollectionsMarshal.AsSpan(_deletionJob.ToClear))
        {
            DebugTools.Assert(_assignedEnts.Remove(index));
        }
        #endif

        if (_deletionJob.ToClear.Count > 16)
        {
            _deletionTask = _parallelManager.Process(_deletionJob, _deletionJob.Count);
            return;
        }

        foreach (var index in CollectionsMarshal.AsSpan(_deletionJob.ToClear))
        {
            ClearEntityPvsData(index);
        }
    }

    private void ReturnEntity(PvsIndex index)
    {
        DebugTools.Assert(!_assignedEnts.Contains(index));
        ref var freeLink = ref _metadataMemory.GetRef<PvsMetadataFreeLink>(index.Index);
        freeLink.NextFree = _dataFreeListHead;
        _dataFreeListHead = index;
    }

    private record struct PvsDeletionsJob(PvsSystem _pvs) : IParallelRobustJob
    {
        public int BatchSize => 8;
        private PvsSystem _pvs = _pvs;
        public List<PvsIndex> ToClear = new();

        public int Count => ToClear.Count;

        public void Execute(int index)
        {
            _pvs.ClearEntityPvsData(ToClear[index]);
        }
    }
}
