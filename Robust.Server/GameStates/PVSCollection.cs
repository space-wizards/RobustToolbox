using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Robust.Server.GameStates;

public interface IPVSCollection
{
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

public class PVSCollection<TIndex> : IPVSCollection where TIndex : IComparable<TIndex>, IEquatable<TIndex>
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

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
        _deletionHistory.Add((tick, index));
        RemoveIndex(index);
    }

    /// <inheritdoc />
    public void CullDeletionHistoryUntil(GameTick tick) => _deletionHistory.RemoveAll(hist => hist.tick < tick);

    #endregion

    #region AddIndex

    /// <summary>
    /// Adds an <see cref="TIndex"/> to be sent to all players at all times.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to add.</param>
    public void AddIndex(TIndex index)
    {

    }

    /// <summary>
    /// Adds an <see cref="TIndex"/> to be sent to a specific <see cref="ICommonSession"/> at all times.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to add.</param>
    /// <param name="session">The <see cref="ICommonSession"/> receiving the object.</param>
    public void AddIndex(TIndex index, ICommonSession session)
    {

    }

    /// <summary>
    /// Adds an <see cref="TIndex"/> to the internal cache based on the provided <see cref="EntityCoordinates"/>.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to add.</param>
    /// <param name="coordinates">The <see cref="EntityCoordinates"/> to use when adding the <see cref="TIndex"/> to the internal cache.</param>
    public void AddIndex(TIndex index, EntityCoordinates coordinates)
    {

    }

    /// <summary>
    /// Adds an <see cref="TIndex"/> to the internal grid-chunk cache using the provided <see cref="gridId"/> and <see cref="chunkIndices"/>.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to add.</param>
    /// <param name="gridId">The id of the grid.</param>
    /// <param name="chunkIndices">The indices of the chunk.</param>
    public void AddIndex(TIndex index, GridId gridId, Vector2i chunkIndices)
    {

    }

    /// <summary>
    /// Adds an <see cref="TIndex"/> to the internal map-chunk cache using the provided <see cref="mapId"/> and <see cref="chunkIndices"/>.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to add.</param>
    /// <param name="mapId">The id of the map.</param>
    /// <param name="chunkIndices">The indices of the mapchunk.</param>
    public void AddIndex(TIndex index, MapId mapId, Vector2i chunkIndices)
    {

    }

    #endregion

    #region UpdateIndex

    /// <summary>
    /// Updates an <see cref="TIndex"/> to be sent to all players at all times.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to update.</param>
    public void UpdateIndex(TIndex index)
    {

    }

    /// <summary>
    /// Updates an <see cref="TIndex"/> to be sent to a specific <see cref="ICommonSession"/> at all times.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to update.</param>
    /// <param name="session">The <see cref="ICommonSession"/> receiving the object.</param>
    public void UpdateIndex(TIndex index, ICommonSession session)
    {

    }

    /// <summary>
    /// Updates an <see cref="TIndex"/> with the location based on the provided <see cref="EntityCoordinates"/>.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to update.</param>
    /// <param name="coordinates">The <see cref="EntityCoordinates"/> to use when adding the <see cref="TIndex"/> to the internal cache.</param>
    public void UpdateIndex(TIndex index, EntityCoordinates coordinates)
    {

    }

    /// <summary>
    /// Updates an <see cref="TIndex"/> using the provided <see cref="gridId"/> and <see cref="chunkIndices"/>.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to update.</param>
    /// <param name="gridId">The id of the grid.</param>
    /// <param name="chunkIndices">The indices of the chunk.</param>
    public void UpdateIndex(TIndex index, GridId gridId, Vector2i chunkIndices)
    {

    }

    /// <summary>
    /// Updates an <see cref="TIndex"/> using the provided <see cref="mapId"/> and <see cref="chunkIndices"/>.
    /// </summary>
    /// <param name="index">The <see cref="TIndex"/> to update.</param>
    /// <param name="mapId">The id of the map.</param>
    /// <param name="chunkIndices">The indices of the mapchunk.</param>
    public void UpdateIndex(TIndex index, MapId mapId, Vector2i chunkIndices)
    {

    }

    #endregion

    #region IndexLocations

    private void RemoveIndex(TIndex index)
    {
        switch (_indexLocations[index])
        {
            case GlobalOverride globalOverride:
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
            default:
                throw new ArgumentOutOfRangeException();
        }

        _indexLocations.Remove(index);
    }

    private abstract record IndexLocation;
    private record MapChunkLocation(MapId MapId, Vector2i ChunkIndices) : IndexLocation;
    private record GridChunkLocation(GridId GridId, Vector2i ChunkIndices) : IndexLocation;
    private record GlobalOverride : IndexLocation;
    private record LocalOverride(ICommonSession Session) : IndexLocation;

    #endregion
}
