using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Map;

internal interface INetworkedMapManager : IMapManagerInternal
{
    GameStateMapData? GetStateData(GameTick fromTick);
    void CullDeletionHistory(GameTick upToTick);

    // Two methods here, so that new grids etc can be made BEFORE entities get states applied,
    // but old ones can be deleted after.
    void ApplyGameStatePre(GameStateMapData? data, ReadOnlySpan<EntityState> entityStates);
    void ApplyGameStatePost(GameStateMapData? data);
}

internal sealed class NetworkedMapManager : MapManager, INetworkedMapManager
{
    private readonly Dictionary<GridId, List<(GameTick tick, Vector2i indices)>> _chunkDeletionHistory = new();
    private readonly List<(GameTick tick, GridId gridId)> _gridDeletionHistory = new();
    private readonly List<(GameTick tick, MapId mapId)> _mapDeletionHistory = new();

    public override void DeleteMap(MapId mapId)
    {
        base.DeleteMap(mapId);
        _mapDeletionHistory.Add((GameTiming.CurTick, mapId));
    }

    public override void DeleteGrid(GridId gridId)
    {
        base.DeleteGrid(gridId);
        _gridDeletionHistory.Add((GameTiming.CurTick, gridId));
        // No point syncing chunk removals anymore!
        _chunkDeletionHistory.Remove(gridId);
    }

    public override void ChunkRemoved(GridId gridId, MapChunk chunk)
    {
        base.ChunkRemoved(gridId, chunk);
        if (!_chunkDeletionHistory.TryGetValue(gridId, out var chunks))
        {
            chunks = new List<(GameTick tick, Vector2i indices)>();
            _chunkDeletionHistory[gridId] = chunks;
        }

        chunks.Add((GameTiming.CurTick, chunk.Indices));

        // Seemed easier than having this method on GridFixtureSystem
        if (!TryGetGrid(gridId, out var grid) ||
            !EntityManager.TryGetComponent(grid.GridEntityId, out PhysicsComponent? body) ||
            chunk.Fixtures.Count == 0)
            return;

        // TODO: Like MapManager injecting this is a PITA so need to work out an easy way to do it.
        // Maybe just add like a PostInject method that gets called way later?
        var fixtureSystem = EntitySystem.Get<FixtureSystem>();

        foreach (var fixture in chunk.Fixtures)
        {
            fixtureSystem.DestroyFixture(body, fixture);
        }
    }

    public GameStateMapData? GetStateData(GameTick fromTick)
    {
        var gridDatums = new Dictionary<GridId, GameStateMapData.GridDatum>();
        foreach (MapGrid grid in GetAllGrids())
        {
            if (grid.LastTileModifiedTick < fromTick)
                continue;

            var deletedChunkData = new List<GameStateMapData.DeletedChunkDatum>();

            if (_chunkDeletionHistory.TryGetValue(grid.Index, out var chunks))
            {
                foreach (var (tick, indices) in chunks)
                {
                    if (tick < fromTick)
                        continue;

                    deletedChunkData.Add(new GameStateMapData.DeletedChunkDatum(indices));
                }
            }

            var chunkData = new List<GameStateMapData.ChunkDatum>();

            foreach (var (index, chunk) in grid.GetMapChunks())
            {
                if (chunk.LastTileModifiedTick < fromTick)
                    continue;

                var tileBuffer = new Tile[grid.ChunkSize * (uint)grid.ChunkSize];

                // Flatten the tile array.
                // NetSerializer doesn't do multi-dimensional arrays.
                // This is probably really expensive.
                for (var x = 0; x < grid.ChunkSize; x++)
                {
                    for (var y = 0; y < grid.ChunkSize; y++)
                    {
                        tileBuffer[x * grid.ChunkSize + y] = chunk.GetTile((ushort)x, (ushort)y);
                    }
                }
                chunkData.Add(new GameStateMapData.ChunkDatum(index, tileBuffer));
            }

            var gridDatum = new GameStateMapData.GridDatum(
                    chunkData.ToArray(),
                    deletedChunkData.ToArray(),
                    new MapCoordinates(grid.WorldPosition, grid.ParentMapId),
                    grid.WorldRotation);

            gridDatums.Add(grid.Index, gridDatum);
        }

        // -- Map Deletion Data --
        var mapDeletionsData = new List<MapId>();

        foreach (var (tick, mapId) in _mapDeletionHistory)
        {
            if (tick < fromTick)
                continue;
            mapDeletionsData.Add(mapId);
        }

        // -- Grid Deletion Data
        var gridDeletionsData = new List<GridId>();

        foreach (var (tick, gridId) in _gridDeletionHistory)
        {
            if (tick < fromTick)
                continue;
            gridDeletionsData.Add(gridId);
        }

        // -- Map Creations --
        var mapCreations = new List<MapId>();

        foreach (var (mapId, tick) in MapCreationTick)
        {
            if (tick < fromTick || mapId == MapId.Nullspace)
                continue;
            mapCreations.Add(mapId);
        }

        // - Grid Creation data --
        var gridCreations = new Dictionary<GridId, GameStateMapData.GridCreationDatum>();

        foreach (MapGrid grid in GetAllGrids())
        {
            if (grid.CreatedTick < fromTick || grid.ParentMapId == MapId.Nullspace)
                continue;
            gridCreations.Add(grid.Index, new GameStateMapData.GridCreationDatum(grid.ChunkSize));
        }

        // no point sending empty collections
        if (gridDatums.Count == 0)
            gridDatums = default;
        if (gridDeletionsData.Count == 0)
            gridDeletionsData = default;
        if (mapDeletionsData.Count == 0)
            mapDeletionsData = default;
        if (mapCreations.Count == 0)
            mapCreations = default;
        if (gridCreations.Count == 0)
            gridCreations = default;

        // no point even creating an empty map state if no data
        if (gridDatums == null && gridDeletionsData == null && mapDeletionsData == null && mapCreations == null && gridCreations == null)
            return default;

        return new GameStateMapData(gridDatums?.ToArray<KeyValuePair<GridId, GameStateMapData.GridDatum>>(), gridDeletionsData?.ToArray(),
            mapDeletionsData?.ToArray(), mapCreations?.ToArray(),
            gridCreations?.ToArray<KeyValuePair<GridId, GameStateMapData.GridCreationDatum>>());
    }

    public void CullDeletionHistory(GameTick upToTick)
    {
        foreach (var (gridId, chunks) in _chunkDeletionHistory.ToArray())
        {
            chunks.RemoveAll(t => t.tick < upToTick);
            if (chunks.Count == 0)
                _chunkDeletionHistory.Remove(gridId);
        }

        _mapDeletionHistory.RemoveAll(t => t.tick < upToTick);
        _gridDeletionHistory.RemoveAll(t => t.tick < upToTick);
    }

    public void ApplyGameStatePre(GameStateMapData? data, ReadOnlySpan<EntityState> entityStates)
    {
        // There was no map data this tick, so nothing to do.
        if (data == null)
            return;

        // First we need to figure out all the NEW MAPS.
        if (data.CreatedMaps != null)
        {
            DebugTools.Assert(!entityStates.IsEmpty, "Received new maps, but no entity state.");

            foreach (var mapId in data.CreatedMaps)
            {
                // map already exists from a previous state.
                if (MapExists(mapId))
                    continue;

                EntityUid mapEuid = default;

                //get shared euid of map comp entity
                foreach (var entityState in entityStates)
                {
                    foreach (var compChange in entityState.ComponentChanges.Span)
                    {
                        if (compChange.State is not MapComponentState mapCompState || mapCompState.MapId != mapId)
                            continue;

                        DebugTools.Assert(compChange.Created, $"new map {mapId} is in CreatedMaps, but compState isn't marked as created.");
                        mapEuid = entityState.Uid;
                        goto BreakMapEntSearch;
                    }
                }

                BreakMapEntSearch:

                DebugTools.Assert(mapEuid != default, $"Could not find corresponding entity state for new map {mapId}.");

                CreateMap(mapId, mapEuid);
            }
        }

        // Then make all the grids.
        if (data.CreatedGrids != null)
        {
            DebugTools.Assert(data.GridData is not null, "Received new grids, but GridData was null.");

            foreach (var (gridId, creationDatum) in data.CreatedGrids)
            {
                if (GridExists(gridId))
                    continue;

                EntityUid gridEuid = default;

                //get shared euid of map comp entity
                foreach (var entityState in entityStates)
                {
                    foreach (var compState in entityState.ComponentChanges.Span)
                    {
                        if (compState.State is not MapGridComponentState gridCompState || gridCompState.GridIndex != gridId)
                            continue;

                        DebugTools.Assert(compState.Created, $"new grid {gridId} is in CreatedGrids, but compState isn't marked as created.");
                        gridEuid = entityState.Uid;
                        goto BreakGridEntSearch;
                    }
                }

                BreakGridEntSearch:

                DebugTools.Assert(gridEuid != default, $"Could not find corresponding entity state for new grid {gridId}.");

                MapId gridMapId = default;
                foreach (var kvData in data.GridData!)
                {
                    if (kvData.Key != gridId)
                        continue;

                    gridMapId = kvData.Value.Coordinates.MapId;
                    break;
                }

                DebugTools.Assert(gridMapId != default, $"Could not find corresponding gridData for new grid {gridId}.");

                CreateGrid(gridMapId, gridId, creationDatum.ChunkSize, gridEuid);
            }
        }

        // Process all grid updates.
        if (data.GridData != null)
        {
            SuppressOnTileChanged = true;
            // Ok good all the grids and maps exist now.
            foreach (var (gridId, gridDatum) in data.GridData)
            {
                var grid = (MapGrid)GetGrid(gridId);
                if (grid.ParentMapId != gridDatum.Coordinates.MapId)
                    throw new NotImplementedException("Moving grids between maps is not yet implemented");

                // I love mapmanager!!!
                grid.WorldPosition = gridDatum.Coordinates.Position;
                grid.WorldRotation = gridDatum.Angle;

                var modified = new List<(Vector2i position, Tile tile)>();
                foreach (var chunkData in gridDatum.ChunkData)
                {
                    var chunk = grid.GetChunk(chunkData.Index);
                    chunk.SuppressCollisionRegeneration = true;
                    DebugTools.Assert(chunkData.TileData.Length == grid.ChunkSize * grid.ChunkSize);

                    var counter = 0;
                    for (ushort x = 0; x < grid.ChunkSize; x++)
                    {
                        for (ushort y = 0; y < grid.ChunkSize; y++)
                        {
                            var tile = chunkData.TileData[counter++];
                            if (chunk.GetTile(x, y) != tile)
                            {
                                chunk.SetTile(x, y, tile);
                                modified.Add((new Vector2i(chunk.X * grid.ChunkSize + x, chunk.Y * grid.ChunkSize + y), tile));
                            }
                        }
                    }
                }

                if (modified.Count != 0)
                    InvokeGridChanged(this, new GridChangedEventArgs(grid, modified));

                foreach (var chunkData in gridDatum.ChunkData)
                {
                    var chunk = grid.GetChunk(chunkData.Index);
                    chunk.SuppressCollisionRegeneration = false;
                    grid.RegenerateCollision(chunk);
                }

                foreach (var chunkData in gridDatum.DeletedChunkData)
                {
                    grid.RemoveChunk(chunkData.Index);
                }
            }

            SuppressOnTileChanged = false;
        }
    }

    public void ApplyGameStatePost(GameStateMapData? data)
    {
        if (data == null) // if there is no data, there is nothing to do!
            return;

        if (data.DeletedGrids != null)
        {
            foreach (var grid in data.DeletedGrids)
            {
                if (GridExists(grid))
                    DeleteGrid(grid);
            }
        }

        if (data.DeletedMaps != null)
        {
            foreach (var map in data.DeletedMaps)
            {
                if (MapExists(map))
                    DeleteMap(map);
            }
        }
    }
}
