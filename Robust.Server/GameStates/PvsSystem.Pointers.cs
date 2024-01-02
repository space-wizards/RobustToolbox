using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

// This class handles the pinned PvsData array. This array stores information about when each entity was last sent to
// each player. This is somewhat faster than using a per-player Dictionary<EntityUid, PvsData>, though it can be less
// memory efficient.
internal sealed partial class PvsSystem
{
    /// <summary>
    /// Pointers to entity data available for use by newly spawned entities.
    /// </summary>
    private Stack<IntPtr> _pointerPool = new();

    /// <summary>
    /// Pointer offsets available for use by new players.
    /// </summary>
    private Stack<int> _playerPool = new();

    // These just exist for debug asserts.
    private HashSet<IntPtr> _assignedEnts = new();
    private HashSet<int> _assignedPlayers = new();
    private IntPtr _baseAddr;

    /// <summary>
    /// Recently returned pointers from deleted entities. These get moved to <see cref="_pendingReturns"/> before
    /// moving back to <see cref="_pointerPool"/>
    /// </summary>
    private List<IntPtr> _incomingReturns = new();

    /// <summary>
    /// Recently returned pointers from deleted entities. These will get returned to the  <see cref="_pointerPool"/>
    /// after a minimum amount of time has passed, to ensure that processing late game-state  ack messages doesn't
    /// write data to deleted entities.
    /// </summary>
    private List<IntPtr> _pendingReturns = new();

    /// <summary>
    /// Tick at which the <see cref="_pendingReturns"/> were last processed.
    /// </summary>
    private GameTick _lastReturn = GameTick.Zero;

    /// <summary>
    /// A pinned PvsData array containing information about what entities have been seen / sent-to each player.
    /// The layout is such that the data for a single player is contiguous. This makes clearing data on entity deletion
    /// somewhat slower, but improves cache locality when sending game states.
    /// </summary>
    private PvsData[]? _data;

    private int _playerCount;
    private int _entityCount;

    private WaitHandle? _deletionTask;

    /// <summary>
    /// Ensure that the pinned <see cref="PvsData"/> array has sufficient capacity.
    /// If it does not, this will grow the array, which will also wipe any old data.
    /// </summary>
    public void EnsureCapacity(int minPlayers, int minEntities)
    {
        if (_entityCount >= minEntities && _playerCount >= minPlayers)
            return;

        var initial = (_entityCount, _playerCount);

        var playerGrowth = _configManager.GetCVar(CVars.NetPvsPlayerGrowth);
        while (_playerCount < minPlayers)
        {
            _playerCount += playerGrowth <= 0 ? _playerCount : playerGrowth;
        }

        var entityGrowth = _configManager.GetCVar(CVars.NetPvsEntityGrowth);
        while (_entityCount < minEntities)
        {
            _entityCount += entityGrowth <= 0 ? _entityCount : entityGrowth;
        }

        // Warning, because if this happens it probably means the initial cvars were poorly configured.
        Log.Warning($"Growing PvsData array from {initial._playerCount}x{initial._entityCount} -> {_playerCount}x{_entityCount}");

        InitializePvsArray(_playerCount, _entityCount);
    }

    /// <summary>
    /// Sets up the pinned array of <see cref="PvsData"/> that is used to track what entities have been seen by each
    /// player.
    /// </summary>
    private void InitializePvsArray(int playerCount, int entityCount)
    {
        ClearPointers();
        _entityCount = 0;
        _playerCount = 0;
        _data = null;

        _entityCount = Math.Max(entityCount, _configManager.GetCVar(CVars.NetPvsEntityInitial));
        _playerCount = Math.Max(playerCount, _configManager.GetCVar(CVars.NetPvsPlayerInitial));
        _playerCount = Math.Max(_playerCount, PlayerData.Count);

        checked
        {
            int length = _entityCount * (_playerCount + 1);
            _data = GC.AllocateArray<PvsData>(length, pinned: true);
        }

        PopulatePointers();
    }

    /// <summary>
    /// Populate the pointer pools and assign pointers all the entities & players.
    /// </summary>
    private unsafe void PopulatePointers()
    {
        DebugTools.AssertEqual(_pointerPool.Count, 0);
        DebugTools.AssertEqual(_playerPool.Count, 0);
        _baseAddr = Marshal.UnsafeAddrOfPinnedArrayElement(_data!, 0);

        _pointerPool.EnsureCapacity(_entityCount);
        var ptr = (PvsData*) Marshal.UnsafeAddrOfPinnedArrayElement(_data!, 0);
        for (var i = _entityCount - 1; i >= 0; i--)
        {
            _pointerPool.Push((IntPtr)(ptr + i));
        }

        _playerPool.EnsureCapacity(_playerCount);
        for (var i = _playerCount; i >= 1; i--) // 0-th "player" is reserved
        {
            _playerPool.Push(i * _entityCount);
        }

        foreach (var session in PlayerData.Values)
        {
            AssignPlayerOffset(session);
        }

        // Reassign metadata pointers
        var enumerator = AllEntityQuery<MetaDataComponent>();
        while (enumerator.MoveNext(out var meta))
        {
            AssignEntityPointer(meta);
        }
    }

    /// <summary>
    /// Remove all stored pointers to entries in the PvsData array.
    /// </summary>
    private void ClearPointers()
    {
        _leaveTask?.WaitOne();
        _leaveTask = null;

        _deletionTask?.WaitOne();
        _deletionTask = null;

        _incomingReturns.Clear();
        _pendingReturns.Clear();
        _deletionJob.ToClear.Clear();
        _pointerPool.Clear();
        _playerPool.Clear();
        _assignedEnts.Clear();
        _assignedPlayers.Clear();

        // Remove all pointers stored in any player's PVS send-histories. Required to avoid accidentally writing to
        // invalid bits of memory while processing late game-state acks. This also forces all players to receive a full
        // game state, in lieu of sending the required PVS leave messages.
        foreach (var session in PlayerData.Values)
        {
            session.Offset = -1;
            ForceFullState(session);
        }
    }

    /// <summary>
    /// Debug assert that verifies that a pointer points to a valid bit of memory in the pinned array.
    /// </summary>
    [Conditional("DEBUG")]
    private void ValidatePtr(IntPtr ptr)
    {
        DebugTools.AssertNotNull(_data);

        var min = Marshal.UnsafeAddrOfPinnedArrayElement(_data!, 0);
        DebugTools.AssertEqual(_baseAddr, min);
        DebugTools.Assert(ptr >= min);

        var max = Marshal.UnsafeAddrOfPinnedArrayElement(_data!, _data!.Length - 1);
        DebugTools.Assert(ptr <= max);
    }

    /// <summary>
    /// This method shuffles the entity pointer pool. This is used to avoid accidental / unrealistic cache locality
    /// in benchmarks.
    /// </summary>
    internal void ShufflePointers(int seed)
    {
        List<IntPtr> ptrs = new(_pointerPool);
        _pointerPool.Clear();

        var rng = new Random(seed);
        var n = ptrs.Count;
        while (n > 0)
        {
            var k = rng.Next(n);
            _pointerPool.Push(ptrs[k]);
            ptrs[k] = ptrs[^1];
            ptrs.RemoveAt(--n);
        }
    }

    /// <summary>
    /// Clear all of this sessions' PvsData for all entities. This effectively means that PVS will act as if the player
    /// had never been sent information about any entity. Used when returning the player's pointer offset to the pool.
    /// </summary>
    private void ClearPlayerPvsData(PvsSession session)
    {
        if (session.Offset <= 0 || _data == null)
            return;

        ValidateOffset(session);
        Array.Clear(_data, session.Offset, _entityCount);
    }

    /// <summary>
    /// Clear all of this entity' PvsData entries. This effectively means that PVS will act as if no player
    /// had never been sent information about this entity. Used when returning the entity's pointer back to the pool.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void ClearEntityPvsData(IntPtr intPtr)
    {
        // TODO PVS is there a faster way to do this?
        // We just need to clear 12-bytes with some fixed stride.
        var ptr = (PvsData*)intPtr;
        for (var i = 0; i <= _playerCount; i++)
        {
            ValidatePtr((IntPtr)ptr);
            ref var data = ref Unsafe.AsRef<PvsData>(ptr);
            data = default;
            ptr += _entityCount;
        }
    }

    /// <summary>
    /// Get the NetEntity associated with a given pointer & session offset.
    /// </summary>
    /// <param name="intPtr">The pointer, with the session offset pre-applied</param>
    /// <param name="session">The session, whose offset needs to be removed</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe NetEntity PtrToNetEntity(IntPtr intPtr, PvsSession session)
    {
        ValidatePtr(intPtr);
        var ptr = ((PvsMetadata*) intPtr) - session.Offset;
        ValidatePtr((IntPtr)ptr);
        DebugTools.Assert(_assignedEnts.Contains((IntPtr)ptr));
        return ptr->NetEntity;
    }

    /// <summary>
    /// Retrieve a player pointer offset from the pool of available offsets and assign it to the player.
    /// </summary>
    private void AssignPlayerOffset(PvsSession session)
    {
        if (session.Offset != -1)
            throw new InvalidOperationException($"Session {session.Session} already has an assigned offset");

        if (!_playerPool.TryPop(out var offset))
        {
            EnsureCapacity(_playerCount + 1, _entityCount);
            // EnsureCapacity will grow the array and re-assign offsets
            DebugTools.Assert(session.Offset > 0);
            return;
        }

        DebugTools.Assert(_assignedPlayers.Add(offset));
        session.Offset = offset;
    }

    /// <summary>
    /// Take a player's assigned pointer offset and return it back to the pool of available player offsets.
    /// </summary>
    private void ReturnPlayerOffset(PvsSession session)
    {
        if (session.Offset <= 0 || _data == null)
            return;

        ClearPlayerPvsData(session);
        DebugTools.Assert(_assignedPlayers.Remove(session.Offset));
        _playerPool.Push(session.Offset);
        session.Offset = -1;
    }

    private void OnEntityAdded(Entity<MetaDataComponent> entity)
    {
        DebugTools.AssertEqual(entity.Comp.PvsData, IntPtr.Zero);

        // Ensure capacity before calling AssignEntityPointer. We do this because growing the array won't automatically
        // assign a pointer to this entity, as the metadata component hasn't technically been added yet.
        if (_pointerPool.Count == 0)
        {
            EnsureCapacity(_playerCount, _entityCount + 1);
            DebugTools.AssertEqual(entity.Comp.PvsData, IntPtr.Zero);
        }

        AssignEntityPointer(entity.Comp);
    }

    /// <summary>
    /// Retrieve a pooled entity pointer and assign it to an entity.
    /// </summary>
    private unsafe void AssignEntityPointer(MetaDataComponent meta)
    {
        if (!_pointerPool.TryPop(out var ptr))
        {
            EnsureCapacity(_playerCount, _entityCount + 1);
            // EnsureCapacity will re-assign pointers to existing entities
            DebugTools.AssertNotEqual(meta.PvsData, IntPtr.Zero);
        }

        ValidatePtr(ptr);
        DebugTools.Assert(_assignedEnts.Add(ptr));
        DebugTools.AssertEqual(((PvsMetadata*) ptr)->NetEntity, NetEntity.Invalid);
        DebugTools.AssertNotEqual(meta.NetEntity, NetEntity.Invalid);

        meta.PvsData = ptr;
        var metaPtr = ((PvsMetadata*) ptr);
        metaPtr->NetEntity = meta.NetEntity;
        metaPtr->LastModifiedTick = meta.LastModifiedTick;
        metaPtr->VisMask = meta.VisibilityMask;
        metaPtr->LifeStage = meta.EntityLifeStage;
    }

    /// <summary>
    /// Return an entity's index in the pinned array back to the pool of available indices.
    /// </summary>
    private void OnEntityDeleted(Entity<MetaDataComponent> entity)
    {
        var ptr = entity.Comp.PvsData;
        entity.Comp.PvsData = default;

        if (ptr == default)
            return;

        ValidatePtr(ptr);
        _incomingReturns.Add(ptr);
    }

    /// <summary>
    /// Immediately return all pointers back to the pool after flushing all entities.
    /// </summary>
    private void AfterEntityFlush()
    {
        if (_data == null)
            return;

        DebugTools.AssertEqual(EntityManager.EntityCount, 0);

        ClearPointers();
        Array.Clear(_data);
        PopulatePointers();
    }

    private void ValidateOffset(PvsSession session)
    {
        if (_data == null)
            return;

        DebugTools.Assert(_assignedPlayers.Contains(session.Offset));

        if (session.Offset <= 0
            || session.Offset % _entityCount != 0
            || session.Offset + _entityCount > _data!.Length)
        {
            PlayerData.Remove(session.Session);
            throw new Exception($"Pvs session {session} has invalid pointer offset");
        }
    }

    /// <summary>
    /// This update method periodically returns entity pointer back to the pool, once we are sure no old
    /// game state acks will use pointers to that entity.
    /// </summary>
    private void ProcessDeletions()
    {
        var curTick = _gameTiming.CurTick;

        if (_data == null)
            return;

        if (curTick < _lastReturn + (uint)ForceAckThreshold + 1)
            return;

        if (curTick < _lastReturn)
            throw new InvalidOperationException($"Time travel is not supported");

        _leaveTask?.WaitOne();
        _leaveTask = null;

        _deletionTask?.WaitOne();
        _deletionTask = null;

        _lastReturn = curTick;

        foreach (var ptr in CollectionsMarshal.AsSpan(_deletionJob.ToClear))
        {
            _pointerPool.Push(ptr);
        }
        _deletionJob.ToClear.Clear();

        // Cycle lists.
        (_deletionJob.ToClear, _pendingReturns, _incomingReturns) = (_pendingReturns, _incomingReturns, _deletionJob.ToClear);

        if (_deletionJob.ToClear.Count == 0)
            return;

        #if DEBUG
        foreach (var ptr in CollectionsMarshal.AsSpan(_deletionJob.ToClear))
        {
            DebugTools.Assert(_assignedEnts.Remove(ptr));
        }
        #endif

        if (_deletionJob.ToClear.Count > 16)
        {
            _deletionTask = _parallelManager.Process(_deletionJob, _deletionJob.Count);
            return;
        }

        foreach (var ptr in CollectionsMarshal.AsSpan(_deletionJob.ToClear))
        {
            ClearEntityPvsData(ptr);
        }
    }

    private record struct PvsDeletionsJob(PvsSystem _pvs) : IParallelRobustJob
    {
        public int BatchSize => 8;
        private PvsSystem _pvs = _pvs;
        public List<IntPtr> ToClear = new();

        public int Count => ToClear.Count;

        public void Execute(int index)
        {
            _pvs.ClearEntityPvsData(ToClear[index]);
        }
    }
}
