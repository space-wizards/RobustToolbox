using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Robust.Server.GameStates;

public interface IPVSCollection
{
    /// <summary>
    /// Processes all previous additions, removals and updates of indices.
    /// </summary>
    public void Process();
    public void AddPlayer(ICommonSession session);
    public void AddGrid(GridId gridId);
    public void AddMap(MapId mapId);

    public void RemovePlayer(ICommonSession session);

    public void RemoveGrid(GridId gridId);

    public void RemoveMap(MapId mapId);

    /// <summary>
    /// Remove all deletions up to a <see cref="GameTick"/>.
    /// </summary>
    /// <param name="tick">The <see cref="GameTick"/> before which all deletions should be removed.</param>
    public void CullDeletionHistoryUntil(GameTick tick);
}

public sealed class PVSCollection<TIndex> : IPVSCollection where TIndex : IComparable<TIndex>, IEquatable<TIndex>
{
    [Shared.IoC.Dependency] private readonly IEntityManager _entityManager = default!;
    [Shared.IoC.Dependency] private readonly IMapManager _mapManager = default!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2i GetChunkIndices(Vector2 coordinates)
    {
        return (coordinates / PVSSystem.ChunkSize).Floored();
    }

    /// <summary>
    /// Index of which <see cref="TIndex"/> are contained in which mapchunk, indexed by <see cref="Vector2i"/>.
    /// </summary>
    private readonly Dictionary<MapId, Dictionary<Vector2i, HashSet<TIndex>>> _mapChunkContents = new();

    /// <summary>
    /// Index of which <see cref="TIndex"/> are contained in which gridchunk, indexed by <see cref="Vector2i"/>.
    /// </summary>
    private readonly Dictionary<GridId, Dictionary<Vector2i, HashSet<TIndex>>> _gridChunkContents = new();

    /// <summary>
    /// List of <see cref="TIndex"/> that should always get sent.
    /// </summary>
    private readonly HashSet<TIndex> _globalOverrides = new();

    /// <summary>
    /// List of <see cref="TIndex"/> that should always get sent.
    /// </summary>
    public HashSet<TIndex>.Enumerator GlobalOverridesEnumerator => _globalOverrides.GetEnumerator();

    /// <summary>
    /// List of <see cref="TIndex"/> that should always get sent to a certain <see cref="ICommonSession"/>.
    /// </summary>
    private readonly Dictionary<ICommonSession, HashSet<TIndex>> _localOverrides = new();

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

    public PVSCollection()
    {
        IoCManager.InjectDependencies(this);
    }

    public void Process()
    {
        var changedIndices = new HashSet<TIndex>(_locationChangeBuffer.Keys);

        var changedChunkLocations = new HashSet<IIndexLocation>();
        foreach (var (index, tick) in _removalBuffer)
        {
            //changes dont need to be computed if we are removing the index anyways
            if (changedIndices.Remove(index) && !_indexLocations.ContainsKey(index))
            {
                //this index wasnt added yet, so we can safely just skip the deletion
                continue;
            }

            var location = RemoveIndexInternal(index);
            if(location is GridChunkLocation or MapChunkLocation)
                changedChunkLocations.Add(location);
            _deletionHistory.Add((tick, index));
        }

        // remove empty chunk-subsets
        foreach (var chunkLocation in changedChunkLocations)
        {
            switch (chunkLocation)
            {
                case GridChunkLocation gridChunkLocation:
                    if (_gridChunkContents[gridChunkLocation.GridId][gridChunkLocation.ChunkIndices].Count == 0)
                        _gridChunkContents[gridChunkLocation.GridId].Remove(gridChunkLocation.ChunkIndices);
                    break;
                case MapChunkLocation mapChunkLocation:
                    if (_mapChunkContents[mapChunkLocation.MapId][mapChunkLocation.ChunkIndices].Count == 0)
                        _mapChunkContents[mapChunkLocation.MapId].Remove(mapChunkLocation.ChunkIndices);
                    break;
            }
        }

        foreach (var index in changedIndices)
        {
            RemoveIndexInternal(index);

            AddIndexInternal(index, _locationChangeBuffer[index]);
        }

        _locationChangeBuffer.Clear();
        _removalBuffer.Clear();
    }

    public bool TryGetChunk(MapId mapId, Vector2i chunkIndices, [NotNullWhen(true)] out HashSet<TIndex>? indices) =>
        _mapChunkContents[mapId].TryGetValue(chunkIndices, out indices);

    public bool TryGetChunk(GridId gridId, Vector2i chunkIndices, [NotNullWhen(true)] out HashSet<TIndex>? indices) =>
        _gridChunkContents[gridId].TryGetValue(chunkIndices, out indices);

    public HashSet<TIndex>.Enumerator GetElementsForSession(ICommonSession session) => _localOverrides[session].GetEnumerator();

    private void AddIndexInternal(TIndex index, IIndexLocation location)
    {
        switch (location)
        {
            case GlobalOverride _:
                _globalOverrides.Add(index);
                break;
            case GridChunkLocation gridChunkLocation:
                // might be gone due to grid-deletions
                if(!_gridChunkContents.ContainsKey(gridChunkLocation.GridId)) return;
                if(!_gridChunkContents[gridChunkLocation.GridId].ContainsKey(gridChunkLocation.ChunkIndices))
                    _gridChunkContents[gridChunkLocation.GridId][gridChunkLocation.ChunkIndices] = new();
                _gridChunkContents[gridChunkLocation.GridId][gridChunkLocation.ChunkIndices].Add(index);
                break;
            case LocalOverride localOverride:
                // might be gone due to disconnects
                if(!_localOverrides.ContainsKey(localOverride.Session)) return;
                _localOverrides[localOverride.Session].Add(index);
                break;
            case MapChunkLocation mapChunkLocation:
                // might be gone due to map-deletions
                if(!_mapChunkContents.ContainsKey(mapChunkLocation.MapId)) return;
                if(!_mapChunkContents[mapChunkLocation.MapId].ContainsKey(mapChunkLocation.ChunkIndices))
                    _mapChunkContents[mapChunkLocation.MapId][mapChunkLocation.ChunkIndices] = new();
                _mapChunkContents[mapChunkLocation.MapId][mapChunkLocation.ChunkIndices].Add(index);
                break;
        }

        // we want this to throw if there is already an entry because if that happens we fucked up somewhere
        _indexLocations.Add(index, location);
    }

    private IIndexLocation? RemoveIndexInternal(TIndex index)
    {
        // the index might be gone due to disconnects/grid-/map-deletions
        if (!_indexLocations.TryGetValue(index, out var location))
            return null;
        // since we can find the index, we can assume the dicts will be there too & dont need to do any checks. gaming.
        switch (location)
        {
            case GlobalOverride _:
                _globalOverrides.Remove(index);
                break;
            case GridChunkLocation gridChunkLocation:
                _gridChunkContents[gridChunkLocation.GridId][gridChunkLocation.ChunkIndices].Remove(index);
                break;
            case LocalOverride localOverride:
                _localOverrides[localOverride.Session].Remove(index);
                break;
            case MapChunkLocation mapChunkLocation:
                _mapChunkContents[mapChunkLocation.MapId][mapChunkLocation.ChunkIndices].Remove(index);
                break;
        }

        _indexLocations.Remove(index);
        return location;
    }

    #region Init Functions

    /// <inheritdoc />
    public void AddPlayer(ICommonSession session)
    {
        _localOverrides[session] = new();
        _lastSeen[session] = new();
    }

    /// <inheritdoc />
    public void AddGrid(GridId gridId) => _gridChunkContents[gridId] = new();

    /// <inheritdoc />
    public void AddMap(MapId mapId) => _mapChunkContents[mapId] = new();

    #endregion

    #region ShutdownFunctions

    /// <inheritdoc />
    public void RemovePlayer(ICommonSession session)
    {
        foreach (var index in _localOverrides[session])
        {
            _indexLocations.Remove(index);
        }
        _localOverrides.Remove(session);
        _lastSeen.Remove(session);
    }

    /// <inheritdoc />
    public void RemoveGrid(GridId gridId)
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
    public void CullDeletionHistoryUntil(GameTick tick) => _deletionHistory.RemoveAll(hist => hist.tick < tick);

    public List<TIndex> GetDeletedIndices(GameTick fromTick)
    {
        var list = new List<TIndex>();
        foreach (var (tick, id) in _deletionHistory)
        {
            if (tick >= fromTick) list.Add(id);
        }

        return list;
    }

    #endregion

    #region UpdateIndex

    private bool IsOverride(TIndex index)
    {
        if (_locationChangeBuffer.TryGetValue(index, out var change) &&
            change is GlobalOverride or LocalOverride) return true;

        if (_indexLocations.TryGetValue(index, out var indexLoc) &&
            indexLoc is GlobalOverride or LocalOverride) return true;

        return false;
    }

    /// <summary>
    /// Updates an <see cref="TIndex"/> to be sent to all players at all times.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to update.</param>
    /// <param name="removeFromOverride">An index at an override position will not be updated unless you set this flag.</param>
    public void UpdateIndex(TIndex index, bool removeFromOverride = false)
    {
        if(!removeFromOverride && IsOverride(index))
            return;

        RegisterUpdate(index, new GlobalOverride());
    }

    /// <summary>
    /// Updates an <see cref="TIndex"/> to be sent to a specific <see cref="ICommonSession"/> at all times.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to update.</param>
    /// <param name="session">The <see cref="ICommonSession"/> receiving the object.</param>
    /// <param name="removeFromOverride">An index at an override position will not be updated unless you set this flag.</param>
    public void UpdateIndex(TIndex index, ICommonSession session, bool removeFromOverride = false)
    {
        if(!removeFromOverride && IsOverride(index))
            return;

        RegisterUpdate(index, new LocalOverride(session));
    }

    /// <summary>
    /// Updates an <see cref="TIndex"/> with the location based on the provided <see cref="EntityCoordinates"/>.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to update.</param>
    /// <param name="coordinates">The <see cref="EntityCoordinates"/> to use when adding the <see cref="TIndex"/> to the internal cache.</param>
    /// <param name="removeFromOverride">An index at an override position will not be updated unless you set this flag.</param>
    public void UpdateIndex(TIndex index, EntityCoordinates coordinates, bool removeFromOverride = false)
    {
        if(!removeFromOverride && IsOverride(index))
            return;

        var gridId = coordinates.GetGridId(_entityManager);
        if (gridId != GridId.Invalid)
        {
            var gridIndices = GetChunkIndices(_mapManager.GetGrid(gridId).LocalToGrid(coordinates));
            UpdateIndex(index, gridId, gridIndices, true); //skip overridecheck bc we already did it (saves some dict lookups)
            return;
        }

        var mapId = coordinates.GetMapId(_entityManager);
        var mapIndices = GetChunkIndices(coordinates.ToMapPos(_entityManager));
        UpdateIndex(index, mapId, mapIndices, true); //skip overridecheck bc we already did it (saves some dict lookups)
    }

    /// <summary>
    /// Updates an <see cref="TIndex"/> using the provided <see cref="gridId"/> and <see cref="chunkIndices"/>.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to update.</param>
    /// <param name="gridId">The id of the grid.</param>
    /// <param name="chunkIndices">The indices of the chunk.</param>
    /// <param name="removeFromOverride">An index at an override position will not be updated unless you set this flag.</param>
    public void UpdateIndex(TIndex index, GridId gridId, Vector2i chunkIndices, bool removeFromOverride = false)
    {
        if(!removeFromOverride && IsOverride(index))
            return;

        RegisterUpdate(index, new GridChunkLocation(gridId, chunkIndices));
    }

    /// <summary>
    /// Updates an <see cref="TIndex"/> using the provided <see cref="mapId"/> and <see cref="chunkIndices"/>.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to update.</param>
    /// <param name="mapId">The id of the map.</param>
    /// <param name="chunkIndices">The indices of the mapchunk.</param>
    /// <param name="removeFromOverride">An index at an override position will not be updated unless you set this flag.</param>
    public void UpdateIndex(TIndex index, MapId mapId, Vector2i chunkIndices, bool removeFromOverride = false)
    {
        if(!removeFromOverride && IsOverride(index))
            return;

        RegisterUpdate(index, new MapChunkLocation(mapId, chunkIndices));
    }

    private void RegisterUpdate(TIndex index, IIndexLocation location)
    {
        if(_indexLocations.TryGetValue(index, out var oldLocation) && oldLocation == location) return;

        _locationChangeBuffer[index] = location;
    }

    #endregion
}

#region IndexLocations

public interface IIndexLocation {};

public struct MapChunkLocation : IIndexLocation
{
    public MapChunkLocation(MapId mapId, Vector2i chunkIndices)
    {
        MapId = mapId;
        ChunkIndices = chunkIndices;
    }

    public MapId MapId { get; init; }
    public Vector2i ChunkIndices { get; init; }
}

public struct GridChunkLocation : IIndexLocation
{
    public GridChunkLocation(GridId gridId, Vector2i chunkIndices)
    {
        GridId = gridId;
        ChunkIndices = chunkIndices;
    }

    public GridId GridId { get; init; }
    public Vector2i ChunkIndices { get; init; }
}

public struct GlobalOverride : IIndexLocation { }

public struct LocalOverride : IIndexLocation
{
    public LocalOverride(ICommonSession session)
    {
        Session = session;
    }

    public ICommonSession Session { get; init; }
}

#endregion
