using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

public interface IPVSCollection
{
    /// <summary>
    /// Processes all previous additions, removals and updates of indices.
    /// </summary>
    public void Process();

    /// <summary>
    ///     Adds a player session to the collection. Returns false if the player was already present.
    /// </summary>
    public bool AddPlayer(ICommonSession session);
    public void AddGrid(EntityUid gridId);
    public void AddMap(MapId mapId);

    /// <summary>
    ///     Removes a player session from the collection. Returns false if the player was not present in the collection.
    /// </summary>
    public bool RemovePlayer(ICommonSession session);

    public void RemoveGrid(EntityUid gridId);

    public void RemoveMap(MapId mapId);

    /// <summary>
    /// Remove all deletions up to a <see cref="GameTick"/>.
    /// </summary>
    /// <param name="tick">The <see cref="GameTick"/> before which all deletions should be removed.</param>
    public void CullDeletionHistoryUntil(GameTick tick);

    public bool IsDirty(IChunkIndexLocation location);

    public bool MarkDirty(IChunkIndexLocation location);

    public void ClearDirty();

}

public sealed class PVSCollection<TIndex> : IPVSCollection where TIndex : IComparable<TIndex>, IEquatable<TIndex>
{
    private readonly IEntityManager _entityManager;
    private readonly SharedTransformSystem _transformSystem;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetChunkIndices(Vector2 coordinates)
    {
        return (coordinates / PvsSystem.ChunkSize).Floored();
    }

    /// <summary>
    /// Index of which <see cref="TIndex"/> are contained in which mapchunk, indexed by <see cref="Vector2i"/>.
    /// </summary>
    private readonly Dictionary<MapId, Dictionary<Vector2i, HashSet<TIndex>>> _mapChunkContents = new();

    /// <summary>
    /// Index of which <see cref="TIndex"/> are contained in which gridchunk, indexed by <see cref="Vector2i"/>.
    /// </summary>
    private readonly Dictionary<EntityUid, Dictionary<Vector2i, HashSet<TIndex>>> _gridChunkContents = new();

    /// <summary>
    /// List of <see cref="TIndex"/> that should always get sent.
    /// </summary>
    private readonly HashSet<TIndex> _globalOverrides = new();

    /// <summary>
    /// List of <see cref="TIndex"/> that should always get sent along with all of their children.
    /// </summary>
    private readonly HashSet<TIndex> _globalRecursiveOverrides = new();

    /// <summary>
    /// List of <see cref="TIndex"/> that should always get sent.
    /// </summary>
    public HashSet<TIndex>.Enumerator GlobalOverridesEnumerator => _globalOverrides.GetEnumerator();

    /// <summary>
    /// List of <see cref="TIndex"/> that should always get sent along with all of their children.
    /// </summary>
    public HashSet<TIndex>.Enumerator GlobalRecursiveOverridesEnumerator => _globalRecursiveOverrides.GetEnumerator();

    /// <summary>
    /// List of <see cref="TIndex"/> that should always get sent to a certain <see cref="ICommonSession"/>.
    /// </summary>
    private readonly Dictionary<ICommonSession, HashSet<TIndex>> _sessionOverrides = new();

    /// <summary>
    /// Which <see cref="TIndex"/> where last seen/sent to a certain <see cref="ICommonSession"/>.
    /// </summary>
    private readonly Dictionary<ICommonSession, HashSet<TIndex>> _lastSeen = new();

    /// <summary>
    /// History of deletion-tuples, containing the <see cref="GameTick"/> of the deletion, as well as the <see cref="TIndex"/> of the object which was deleted.
    /// </summary>
    private readonly List<(GameTick tick, TIndex index)> _deletionHistory = new();

    /// <summary>
    /// An index containing the <see cref="IIndexLocation"/>s of all <see cref="TIndex"/>.
    /// </summary>
    private readonly Dictionary<TIndex, IIndexLocation> _indexLocations = new();

    /// <summary>
    /// Buffer of all locationchanges since the last process call
    /// </summary>
    private readonly Dictionary<TIndex, IIndexLocation> _locationChangeBuffer = new();
    /// <summary>
    /// Buffer of all indexremovals since the last process call
    /// </summary>
    private readonly Dictionary<TIndex, GameTick> _removalBuffer = new();

    /// <summary>
    /// To avoid re-allocating the hashset every tick we'll just store it.
    /// </summary>
    private HashSet<TIndex> _changedIndices = new();

    /// <summary>
    /// A set of all chunks changed last tick
    /// </summary>
    private HashSet<IChunkIndexLocation> _dirtyChunks = new();

    private ISawmill _sawmill;

    public PVSCollection(ISawmill sawmill, IEntityManager entityManager, SharedTransformSystem transformSystem)
    {
        _sawmill = sawmill;
        _entityManager = entityManager;
        _transformSystem = transformSystem;
    }

    public void Process()
    {
        _changedIndices.EnsureCapacity(_locationChangeBuffer.Count);

        foreach (var key in _locationChangeBuffer.Keys)
        {
            _changedIndices.Add(key);
        }

        foreach (var (index, tick) in _removalBuffer)
        {
            _deletionHistory.Add((tick, index));
            _changedIndices.Remove(index);
            var location = RemoveIndexInternal(index);
            if (location == null)
                continue;

            if(location is GridChunkLocation or MapChunkLocation)
                _dirtyChunks.Add((IChunkIndexLocation) location);
        }

        foreach (var index in _changedIndices)
        {
            var oldLoc = RemoveIndexInternal(index);
            if(oldLoc is GridChunkLocation or MapChunkLocation)
                _dirtyChunks.Add((IChunkIndexLocation) oldLoc);

            AddIndexInternal(index, _locationChangeBuffer[index], _dirtyChunks);
        }

        // remove empty chunk-subsets
        foreach (var chunkLocation in _dirtyChunks)
        {
            switch (chunkLocation)
            {
                case GridChunkLocation gridChunkLocation:
                    if(!_gridChunkContents.TryGetValue(gridChunkLocation.GridId, out var gridChunks)) continue;
                    if(!gridChunks.TryGetValue(gridChunkLocation.ChunkIndices, out var chunk)) continue;
                    if(chunk.Count == 0)
                        gridChunks.Remove(gridChunkLocation.ChunkIndices);
                    break;
                case MapChunkLocation mapChunkLocation:
                    if(!_mapChunkContents.TryGetValue(mapChunkLocation.MapId, out var mapChunks)) continue;
                    if(!mapChunks.TryGetValue(mapChunkLocation.ChunkIndices, out chunk)) continue;
                    if(chunk.Count == 0)
                        mapChunks.Remove(mapChunkLocation.ChunkIndices);
                    break;
            }
        }

        _changedIndices.Clear();
        _locationChangeBuffer.Clear();
        _removalBuffer.Clear();
    }

    public bool IsDirty(IChunkIndexLocation location) => _dirtyChunks.Contains(location);

    public bool MarkDirty(IChunkIndexLocation location) => _dirtyChunks.Add(location);

    public void ClearDirty() => _dirtyChunks.Clear();

    public bool TryGetChunk(MapId mapId, Vector2i chunkIndices, [NotNullWhen(true)] out HashSet<TIndex>? indices) =>
        _mapChunkContents[mapId].TryGetValue(chunkIndices, out indices);

    public bool TryGetChunk(EntityUid gridId, Vector2i chunkIndices, [NotNullWhen(true)] out HashSet<TIndex>? indices) =>
        _gridChunkContents[gridId].TryGetValue(chunkIndices, out indices);

    public HashSet<TIndex>.Enumerator GetSessionOverrides(ICommonSession session) => _sessionOverrides[session].GetEnumerator();

    private void AddIndexInternal(TIndex index, IIndexLocation location, HashSet<IChunkIndexLocation> dirtyChunks)
    {
        switch (location)
        {
            case GlobalOverride global:
                if (global.Recursive)
                    _globalRecursiveOverrides.Add(index);
                else
                    _globalOverrides.Add(index);
                break;
            case GridChunkLocation gridChunkLocation:
                // might be gone due to grid-deletions
                if(!_gridChunkContents.TryGetValue(gridChunkLocation.GridId, out var gridChunk)) return;
                var gridLoc = gridChunk.GetOrNew(gridChunkLocation.ChunkIndices);
                gridLoc.Add(index);
                dirtyChunks.Add(gridChunkLocation);
                break;
            case SessionOverride sessionOverride:
                if (!_sessionOverrides.TryGetValue(sessionOverride.Session, out var set))
                    return;
                set.Add(index);
                break;
            case MapChunkLocation mapChunkLocation:
                // might be gone due to map-deletions
                if(!_mapChunkContents.TryGetValue(mapChunkLocation.MapId, out var mapChunk)) return;
                var mapLoc = mapChunk.GetOrNew(mapChunkLocation.ChunkIndices);
                mapLoc.Add(index);
                dirtyChunks.Add(mapChunkLocation);
                break;
        }

        // we want this to throw if there is already an entry because if that happens we fucked up somewhere
        _indexLocations.Add(index, location);
    }

    private IIndexLocation? RemoveIndexInternal(TIndex index)
    {
        // the index might be gone due to disconnects/grid-/map-deletions
        if (!_indexLocations.Remove(index, out var location))
            return null;
        // since we can find the index, we can assume the dicts will be there too & dont need to do any checks. gaming.
        switch (location)
        {
            case GlobalOverride global:
                var set = global.Recursive ? _globalRecursiveOverrides : _globalOverrides;
                set.Remove(index);
                break;
            case GridChunkLocation gridChunkLocation:
                _gridChunkContents[gridChunkLocation.GridId][gridChunkLocation.ChunkIndices].Remove(index);
                break;
            case SessionOverride sessionOverride:
                _sessionOverrides.GetValueOrDefault(sessionOverride.Session)?.Remove(index);
                break;
            case MapChunkLocation mapChunkLocation:
                _mapChunkContents[mapChunkLocation.MapId][mapChunkLocation.ChunkIndices].Remove(index);
                break;
        }
        return location;
    }

    #region Init Functions

    /// <inheritdoc />
    public bool AddPlayer(ICommonSession session)
    {
        return _sessionOverrides.TryAdd(session, new()) & _lastSeen.TryAdd(session, new());
    }

    /// <inheritdoc />
    public void AddGrid(EntityUid gridId) => _gridChunkContents[gridId] = new();

    /// <inheritdoc />
    public void AddMap(MapId mapId) => _mapChunkContents[mapId] = new();

    #endregion

    #region ShutdownFunctions

    /// <inheritdoc />
    public bool RemovePlayer(ICommonSession session)
    {
        if (_sessionOverrides.Remove(session, out var indices))
        {
            foreach (var index in indices)
            {
                _indexLocations.Remove(index);
            }
        }

        return _lastSeen.Remove(session) && indices != null;
    }

    /// <inheritdoc />
    public void RemoveGrid(EntityUid gridId)
    {
        foreach (var (_, indices) in _gridChunkContents[gridId])
        {
            foreach (var index in indices)
            {
                _indexLocations.Remove(index);
            }
        }
        _gridChunkContents.Remove(gridId);
    }

    /// <inheritdoc />
    public void RemoveMap(MapId mapId)
    {
        foreach (var (_, indices) in _mapChunkContents[mapId])
        {
            foreach (var index in indices)
            {
                _indexLocations.Remove(index);
            }
        }
        _mapChunkContents.Remove(mapId);
    }

    #endregion

    #region DeletionHistory & RemoveIndex

    /// <summary>
    /// Registers a deletion of an <see cref="TIndex"/> on a <see cref="GameTick"/>. WARNING: this also clears the index out of the internal cache!!!
    /// </summary>
    /// <param name="tick">The <see cref="GameTick"/> at which the deletion took place.</param>
    /// <param name="index">The <see cref="TIndex"/> of the removed object.</param>
    public void RemoveIndex(GameTick tick, TIndex index)
    {
        _removalBuffer[index] = tick;
    }

    /// <inheritdoc />
    public void CullDeletionHistoryUntil(GameTick tick)
    {
        if (tick == GameTick.MaxValue)
        {
            _deletionHistory.Clear();
            return;
        }

        for (var i = _deletionHistory.Count - 1; i >= 0; i--)
        {
            var hist = _deletionHistory[i].tick;
            if (hist <= tick)
            {
                _deletionHistory.RemoveSwap(i);
                if (_largestCulled < hist)
                    _largestCulled = hist;
            }
        }
    }

    private GameTick _largestCulled;

    public List<TIndex>? GetDeletedIndices(GameTick fromTick)
    {
        if (fromTick == GameTick.Zero)
            return null;

        // I'm 99% sure this can never happen, but it is hard to test real laggy/lossy networks with many players.
        if (_largestCulled > fromTick)
        {
            _sawmill.Error($"Culled required deletion history! culled: {_largestCulled}. requested: > {fromTick}");
            _largestCulled = GameTick.Zero;
        }

        var list = new List<TIndex>();
        foreach (var (tick, id) in _deletionHistory)
        {
            if (tick > fromTick)
                list.Add(id);
        }

        return list.Count > 0 ? list : null;
    }

    #endregion

    #region UpdateIndex

    private bool TryGetLocation(TIndex index, out IIndexLocation? location)
    {
        return _locationChangeBuffer.TryGetValue(index, out location)
               || _indexLocations.TryGetValue(index, out location);
    }

    /// <summary>
    /// Updates an <see cref="TIndex"/> to be sent to all players at all times.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to update.</param>
    /// <param name="removeFromOverride">An index at an override position will not be updated unless you set this flag.</param>
    /// <param name="recursive">If true, this will also recursively send any children of the given index.</param>
    public void AddGlobalOverride(TIndex index, bool removeFromOverride, bool recursive)
    {
        if (!TryGetLocation(index, out var oldLocation))
        {
            RegisterUpdate(index, new GlobalOverride(recursive));
            return;
        }

        if (!removeFromOverride && oldLocation is SessionOverride)
            return;

        if (oldLocation is GlobalOverride global &&
            (!removeFromOverride || global.Recursive == recursive))
        {
            return;
        }

        RegisterUpdate(index, new GlobalOverride(recursive));
    }

    /// <summary>
    /// Updates an <see cref="TIndex"/> to be sent to a specific <see cref="ICommonSession"/> at all times.
    /// This will always also send all children of the given entity.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to update.</param>
    /// <param name="session">The <see cref="ICommonSession"/> receiving the object.</param>
    /// <param name="removeFromOverride">An index at an override position will not be updated unless you set this flag.</param>
    public void AddSessionOverride(TIndex index, ICommonSession session, bool removeFromOverride)
    {
        if (!TryGetLocation(index, out var oldLocation))
        {
            RegisterUpdate(index, new SessionOverride(session));
            return;
        }

        if (!removeFromOverride && oldLocation is GlobalOverride)
            return;

        if (oldLocation is SessionOverride local &&
            (!removeFromOverride || local.Session == session))
        {
            return;
        }

        RegisterUpdate(index, new SessionOverride(session));
    }

    /// <summary>
    /// Updates an <see cref="TIndex"/> with the location based on the provided <see cref="EntityCoordinates"/>.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to update.</param>
    /// <param name="coordinates">The <see cref="EntityCoordinates"/> to use when adding the <see cref="TIndex"/> to the internal cache.</param>
    /// <param name="removeFromOverride">An index at an override position will not be updated unless you set this flag.</param>
    public void UpdateIndex(TIndex index, EntityCoordinates coordinates, bool removeFromOverride = false)
    {
        if (!removeFromOverride
            && TryGetLocation(index, out var oldLocation)
            && oldLocation is GlobalOverride or SessionOverride)
        {
            return;
        }

        if (!_entityManager.TryGetComponent(coordinates.EntityId, out TransformComponent? xform))
            return;

        if (xform.GridUid is { } gridId && gridId.IsValid())
        {
            var gridIndices = GetChunkIndices(coordinates.Position);
            UpdateIndex(index, gridId, gridIndices, true); //skip overridecheck bc we already did it (saves some dict lookups)
            return;
        }

        var worldPos = _transformSystem.GetWorldMatrix(xform).Transform(coordinates.Position);
        var mapIndices = GetChunkIndices(worldPos);
        UpdateIndex(index, xform.MapID, mapIndices, true); //skip overridecheck bc we already did it (saves some dict lookups)
    }

    public IChunkIndexLocation GetChunkIndex(EntityCoordinates coordinates)
    {
        if (!_entityManager.TryGetComponent(coordinates.EntityId, out TransformComponent? xform))
            return new MapChunkLocation(default, default);

        if (xform.GridUid is { } gridId && gridId.IsValid())
        {
            var gridIndices = GetChunkIndices(coordinates.Position);
            return new GridChunkLocation(gridId, gridIndices);
        }

        var worldPos = _transformSystem.GetWorldMatrix(xform).Transform(coordinates.Position);
        var mapIndices = GetChunkIndices(worldPos);
        return new MapChunkLocation(xform.MapID, mapIndices);
    }

    /// <summary>
    /// Updates an <see cref="TIndex"/> using the provided <see cref="gridId"/> and <see cref="chunkIndices"/>.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to update.</param>
    /// <param name="gridId">The id of the grid.</param>
    /// <param name="chunkIndices">The indices of the chunk.</param>
    /// <param name="removeFromOverride">An index at an override position will not be updated unless you set this flag.</param>
    /// <param name="forceDirty">If true, this will mark the previous chunk as dirty even if the entity did not move from that chunk.</param>
    public void UpdateIndex(TIndex index, EntityUid gridId, Vector2i chunkIndices, bool removeFromOverride = false, bool forceDirty = false)
    {
        _locationChangeBuffer.TryGetValue(index, out var bufferedLocation);
        _indexLocations.TryGetValue(index, out var oldLocation);

        //removeFromOverride is false 99% of the time.
        if ((bufferedLocation ?? oldLocation) is GlobalOverride or SessionOverride && !removeFromOverride)
            return;

        if (oldLocation is GridChunkLocation oldGrid &&
            oldGrid.ChunkIndices == chunkIndices &&
            oldGrid.GridId == gridId)
        {
            _locationChangeBuffer.Remove(index);

            if (forceDirty)
                _dirtyChunks.Add(oldGrid);
            return;
        }

        RegisterUpdate(index, new GridChunkLocation(gridId, chunkIndices));
    }

    /// <summary>
    /// Updates an <see cref="TIndex"/> using the provided <see cref="mapId"/> and <see cref="chunkIndices"/>.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to update.</param>
    /// <param name="mapId">The id of the map.</param>
    /// <param name="chunkIndices">The indices of the mapchunk.</param>
    /// <param name="removeFromOverride">An index at an override position will not be updated unless you set this flag.</param>
    /// <param name="forceDirty">If true, this will mark the previous chunk as dirty even if the entity did not move from that chunk.</param>
    public void UpdateIndex(TIndex index, MapId mapId, Vector2i chunkIndices, bool removeFromOverride = false, bool forceDirty = false)
    {
        _locationChangeBuffer.TryGetValue(index, out var bufferedLocation);
        _indexLocations.TryGetValue(index, out var oldLocation);

        //removeFromOverride is false 99% of the time.
        if ((bufferedLocation ?? oldLocation) is GlobalOverride or SessionOverride && !removeFromOverride)
            return;

        // Is this entity just returning to its old location?
        if (oldLocation is MapChunkLocation oldMap &&
            oldMap.ChunkIndices == chunkIndices &&
            oldMap.MapId == mapId)
        {
            if (bufferedLocation != null)
                _locationChangeBuffer.Remove(index);

            if (forceDirty)
                _dirtyChunks.Add(oldMap);
            return;
        }

        RegisterUpdate(index, new MapChunkLocation(mapId, chunkIndices));
    }

    private void RegisterUpdate(TIndex index, IIndexLocation location)
    {
        _locationChangeBuffer[index] = location;
    }

    #endregion
}

#region IndexLocations

public interface IIndexLocation {};

public interface IChunkIndexLocation{ };

public struct MapChunkLocation : IIndexLocation, IChunkIndexLocation, IEquatable<MapChunkLocation>
{
    public MapChunkLocation(MapId mapId, Vector2i chunkIndices)
    {
        MapId = mapId;
        ChunkIndices = chunkIndices;
    }

    public MapId MapId { get; init; }
    public Vector2i ChunkIndices { get; init; }

    public bool Equals(MapChunkLocation other)
    {
        return MapId.Equals(other.MapId) && ChunkIndices.Equals(other.ChunkIndices);
    }

    public override bool Equals(object? obj)
    {
        return obj is MapChunkLocation other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MapId, ChunkIndices);
    }
}

public struct GridChunkLocation : IIndexLocation, IChunkIndexLocation, IEquatable<GridChunkLocation>
{
    public GridChunkLocation(EntityUid gridId, Vector2i chunkIndices)
    {
        GridId = gridId;
        ChunkIndices = chunkIndices;
    }

    public EntityUid GridId { get; init; }
    public Vector2i ChunkIndices { get; init; }

    public bool Equals(GridChunkLocation other)
    {
        return GridId.Equals(other.GridId) && ChunkIndices.Equals(other.ChunkIndices);
    }

    public override bool Equals(object? obj)
    {
        return obj is GridChunkLocation other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GridId, ChunkIndices);
    }
}

public struct GlobalOverride : IIndexLocation
{
    /// <summary>
    /// If true, this will also send all children of the override.
    /// </summary>
    public readonly bool Recursive;

    public GlobalOverride(bool recursive)
    {
        Recursive = recursive;
    }
}

public struct SessionOverride : IIndexLocation
{
    public SessionOverride(ICommonSession session)
    {
        Session = session;
    }

    public readonly ICommonSession Session;
}

#endregion
