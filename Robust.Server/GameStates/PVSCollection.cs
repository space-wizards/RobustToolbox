using System;
using System.Collections.Generic;
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

public class PVSCollection<TIndex, TElement> : IPVSCollection where TIndex : IComparable<TIndex>, IEquatable<TIndex>
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public const float ChunkSize = 16;

    public static Vector2i GetChunkIndices(Vector2 coordinates)
    {
        coordinates /= ChunkSize;
        return new Vector2i((int)Math.Floor(coordinates.X), (int)Math.Floor(coordinates.Y));
    }

    /// <summary>
    /// A delegate to retrieve elements
    /// </summary>
    private readonly Func<TIndex, TElement> _getElementDelegate;

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
    /// An index containing the <see cref="IndexLocation"/>s of all <see cref="TIndex"/>.
    /// </summary>
    private readonly Dictionary<TIndex, IndexLocation> _indexLocations = new();

    /// <summary>
    /// Buffer of all indexadditions since the last process call
    /// </summary>
    private readonly Dictionary<TIndex, IndexLocation> _additionBuffer = new();
    /// <summary>
    /// Buffer of all locationchanges since the last process call
    /// </summary>
    private readonly Dictionary<TIndex, IndexLocation> _locationChangeBuffer = new();
    /// <summary>
    /// Buffer of all indexremovals since the last process call
    /// </summary>
    private readonly Dictionary<TIndex, GameTick> _removalBuffer = new();

    public PVSCollection(Func<TIndex, TElement> getElementDelegate)
    {
        _getElementDelegate = getElementDelegate;
        IoCManager.InjectDependencies(this);
    }

    public void Process()
    {
        var addedIndices = new HashSet<TIndex>(_additionBuffer.Keys);
        var changedIndices = new HashSet<TIndex>(_locationChangeBuffer.Keys);

        var changedChunkLocations = new HashSet<IndexLocation>();
        foreach (var (index, tick) in _removalBuffer)
        {
            // if the index was just added, we can just remove it from the added buffer & forget about the removal
            if(addedIndices.Remove(index)) continue;

            //changes dont need to be computed if we are removing it anyways
            changedIndices.Remove(index);

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
            // if the index was just added, we can just override the add entry with this latest locationchange
            if (addedIndices.Contains(index))
            {
                addedIndices.Add(index);
                _additionBuffer[index] = _locationChangeBuffer[index];
                continue;
            }

            RemoveIndexInternal(index);

            AddIndexInternal(index, _locationChangeBuffer[index]);
        }

        foreach (var index in addedIndices)
        {
            AddIndexInternal(index, _additionBuffer[index]);
        }

        _additionBuffer.Clear();
        _locationChangeBuffer.Clear();
        _removalBuffer.Clear();
    }

    public HashSet<TIndex> GetElementsInViewport(IMapManager mapManager, Box2 viewportInMapspace, MapId mapId, Func<TIndex, bool>? validDelegate = null)
    {
        var set = new HashSet<TIndex>();
        GetElementsInViewport(mapManager, viewportInMapspace, mapId, set, validDelegate);
        return set;
    }

    public void GetElementsInViewport(IMapManager mapManager, Box2 viewportInMapspace, MapId mapId, HashSet<TIndex> elementSet, Func<TIndex, bool>? validDelegate = null)
    {
        var topLeft = (viewportInMapspace.TopLeft / ChunkSize).Floored();
        var bottomRight = (viewportInMapspace.BottomRight / ChunkSize).Floored();

        void Add(TIndex index)
        {
            if (validDelegate == null || validDelegate(index))
                elementSet.Add(index);
        }

        for (int x = topLeft.X; x < bottomRight.X; x++)
        {
            for (int y = topLeft.Y; y < bottomRight.Y; y++)
            {
                if (_mapChunkContents[mapId].TryGetValue(new Vector2i(x, y), out var chunk))
                {
                    foreach (var index in chunk)
                    {
                        Add(index);
                    }
                }
            }
        }

        mapManager.FindGridsIntersectingEnumerator(mapId, viewportInMapspace, out var gridEnumerator, true);
        while (gridEnumerator.MoveNext(out var mapGrid))
        {
            if(_gridChunkContents[mapGrid.Index].Count == 0) continue;

            ((IMapGridInternal)mapGrid).GetMapChunks(viewportInMapspace, out var gridChunkEnumerator);

            while (gridChunkEnumerator.MoveNext(out var gridChunk))
            {
                if (_gridChunkContents[mapGrid.Index].TryGetValue(gridChunk.Indices, out var chunk))
                {
                    foreach (var index in chunk)
                    {
                        Add(index);
                    }
                }
            }
        }
    }

    private void AddIndexInternal(TIndex index, IndexLocation location)
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

    private IndexLocation? RemoveIndexInternal(TIndex index)
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

    #region AddIndex

    /// <summary>
    /// Adds an <see cref="TIndex"/> to be sent to all players at all times.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to add.</param>
    public void AddIndex(TIndex index)
    {
        _additionBuffer[index] = new GlobalOverride();
    }

    /// <summary>
    /// Adds an <see cref="TIndex"/> to be sent to a specific <see cref="ICommonSession"/> at all times.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to add.</param>
    /// <param name="session">The <see cref="ICommonSession"/> receiving the object.</param>
    public void AddIndex(TIndex index, ICommonSession session)
    {
        _additionBuffer[index] = new LocalOverride(session);
    }

    /// <summary>
    /// Adds an <see cref="TIndex"/> to the internal cache based on the provided <see cref="EntityCoordinates"/>.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to add.</param>
    /// <param name="coordinates">The <see cref="EntityCoordinates"/> to use when adding the <see cref="TIndex"/> to the internal cache.</param>
    public void AddIndex(TIndex index, EntityCoordinates coordinates)
    {
        var gridId = coordinates.GetGridId(_entityManager);
        var indices = GetChunkIndices(coordinates.Position);
        if (gridId != GridId.Invalid)
        {
            AddIndex(index, gridId, indices);
            return;
        }

        var mapId = coordinates.GetMapId(_entityManager);
        AddIndex(index, mapId, indices);
    }

    /// <summary>
    /// Adds an <see cref="TIndex"/> to the internal grid-chunk cache using the provided <see cref="gridId"/> and <see cref="chunkIndices"/>.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to add.</param>
    /// <param name="gridId">The id of the grid.</param>
    /// <param name="chunkIndices">The indices of the chunk.</param>
    public void AddIndex(TIndex index, GridId gridId, Vector2i chunkIndices)
    {
        _additionBuffer[index] = new GridChunkLocation(gridId, chunkIndices);
    }

    /// <summary>
    /// Adds an <see cref="TIndex"/> to the internal map-chunk cache using the provided <see cref="mapId"/> and <see cref="chunkIndices"/>.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to add.</param>
    /// <param name="mapId">The id of the map.</param>
    /// <param name="chunkIndices">The indices of the mapchunk.</param>
    public void AddIndex(TIndex index, MapId mapId, Vector2i chunkIndices)
    {
        _additionBuffer[index] = new MapChunkLocation(mapId, chunkIndices);
    }

    #endregion

    #region UpdateIndex

    /// <summary>
    /// Updates an <see cref="TIndex"/> to be sent to all players at all times.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to update.</param>
    public void UpdateIndex(TIndex index)
    {
        _locationChangeBuffer[index] = new GlobalOverride();
    }

    /// <summary>
    /// Updates an <see cref="TIndex"/> to be sent to a specific <see cref="ICommonSession"/> at all times.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to update.</param>
    /// <param name="session">The <see cref="ICommonSession"/> receiving the object.</param>
    public void UpdateIndex(TIndex index, ICommonSession session)
    {
        _locationChangeBuffer[index] = new LocalOverride(session);
    }

    /// <summary>
    /// Updates an <see cref="TIndex"/> with the location based on the provided <see cref="EntityCoordinates"/>.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to update.</param>
    /// <param name="coordinates">The <see cref="EntityCoordinates"/> to use when adding the <see cref="TIndex"/> to the internal cache.</param>
    public void UpdateIndex(TIndex index, EntityCoordinates coordinates)
    {
        var gridId = coordinates.GetGridId(_entityManager);
        var indices = GetChunkIndices(coordinates.Position);
        if (gridId != GridId.Invalid)
        {
            UpdateIndex(index, gridId, indices);
            return;
        }

        var mapId = coordinates.GetMapId(_entityManager);
        UpdateIndex(index, mapId, indices);
    }

    /// <summary>
    /// Updates an <see cref="TIndex"/> using the provided <see cref="gridId"/> and <see cref="chunkIndices"/>.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to update.</param>
    /// <param name="gridId">The id of the grid.</param>
    /// <param name="chunkIndices">The indices of the chunk.</param>
    public void UpdateIndex(TIndex index, GridId gridId, Vector2i chunkIndices)
    {
        _locationChangeBuffer[index] = new GridChunkLocation(gridId, chunkIndices);
    }

    /// <summary>
    /// Updates an <see cref="TIndex"/> using the provided <see cref="mapId"/> and <see cref="chunkIndices"/>.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to update.</param>
    /// <param name="mapId">The id of the map.</param>
    /// <param name="chunkIndices">The indices of the mapchunk.</param>
    public void UpdateIndex(TIndex index, MapId mapId, Vector2i chunkIndices)
    {
        _locationChangeBuffer[index] = new MapChunkLocation(mapId, chunkIndices);
    }

    #endregion

    #region IndexLocations

    private abstract record IndexLocation;
    private record MapChunkLocation(MapId MapId, Vector2i ChunkIndices) : IndexLocation;
    private record GridChunkLocation(GridId GridId, Vector2i ChunkIndices) : IndexLocation;
    private record GlobalOverride : IndexLocation;
    private record LocalOverride(ICommonSession Session) : IndexLocation;

    #endregion
}
