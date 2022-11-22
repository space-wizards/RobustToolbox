using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map.Components;
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
}

internal sealed class NetworkedMapManager : MapManager, INetworkedMapManager
{
    public GameStateMapData? GetStateData(GameTick fromTick)
    {
        var gridDatums = new Dictionary<EntityUid, GameStateMapData.GridDatum>();
        var enumerator = EntityManager.AllEntityQueryEnumerator<MapGridComponent>();

        while (enumerator.MoveNext(out var iGrid))
        {
            if (iGrid.LastTileModifiedTick < fromTick)
                continue;

            var chunkData = new List<GameStateMapData.ChunkDatum>();
            var chunks = iGrid.ChunkDeletionHistory;

            foreach (var (tick, indices) in chunks)
            {
                if (tick < fromTick)
                    continue;

                chunkData.Add(GameStateMapData.ChunkDatum.CreateDeleted(indices));
            }

            foreach (var (index, chunk) in iGrid.GetMapChunks())
            {
                if (chunk.LastTileModifiedTick < fromTick)
                    continue;

                var tileBuffer = new Tile[iGrid.ChunkSize * (uint) iGrid.ChunkSize];

                // Flatten the tile array.
                // NetSerializer doesn't do multi-dimensional arrays.
                // This is probably really expensive.
                for (var x = 0; x < iGrid.ChunkSize; x++)
                {
                    for (var y = 0; y < iGrid.ChunkSize; y++)
                    {
                        tileBuffer[x * iGrid.ChunkSize + y] = chunk.GetTile((ushort)x, (ushort)y);
                    }
                }
                chunkData.Add(GameStateMapData.ChunkDatum.CreateModified(index, tileBuffer));
            }

            var gridXform = EntityManager.GetComponent<TransformComponent>(iGrid.GridEntityId);
            var (worldPos, worldRot) = gridXform.GetWorldPositionRotation();

            var gridDatum = new GameStateMapData.GridDatum(
                chunkData.ToArray(),
                new MapCoordinates(worldPos, gridXform.MapID),
                worldRot);

            gridDatums.Add(iGrid.GridEntityId, gridDatum);
        }

        // no point sending empty collections
        if (gridDatums.Count == 0)
            return default;

        return new GameStateMapData(gridDatums.ToArray<KeyValuePair<EntityUid, GameStateMapData.GridDatum>>());
    }

    public void CullDeletionHistory(GameTick upToTick)
    {
        var query = EntityManager.EntityQueryEnumerator<MapGridComponent>();

        while (query.MoveNext(out var grid))
        {
            var chunks = grid.ChunkDeletionHistory;
            chunks.RemoveAll(t => t.tick < upToTick);
        }
    }

    private readonly List<(MapId mapId, EntityUid euid)> _newMaps = new();
    private List<(MapId mapId, EntityUid euid, ushort chunkSize)> _newGrids = new();

    public void ApplyGameStatePre(GameStateMapData? data, ReadOnlySpan<EntityState> entityStates)
    {
        // Setup new maps and grids
        {
            // search for any newly created map components
            foreach (var entityState in entityStates)
            {
                foreach (var compChange in entityState.ComponentChanges.Span)
                {
                    if (compChange.State is MapComponentState mapCompState)
                    {
                        var mapEuid = entityState.Uid;
                        var mapId = mapCompState.MapId;

                        // map already exists from a previous state.
                        if (MapExists(mapId))
                            continue;

                        _newMaps.Add((mapId, mapEuid));
                    }
                    else if (data != null && data.GridData != null && compChange.State is MapGridComponentState gridCompState)
                    {
                        var gridEuid = entityState.Uid;
                        var chunkSize = gridCompState.ChunkSize;

                        // grid already exists from a previous state
                        if(GridExists(gridEuid))
                            continue;

                        // Existing ent?
                        // I love NetworkedMapManager
                        if (EntityManager.EntityExists(gridEuid))
                        {
                            EntityManager.AddComponent<MapGridComponent>(gridEuid);
                            continue;
                        }

                        DebugTools.Assert(chunkSize > 0, $"Invalid chunk size in entity state for new grid {gridEuid}.");

                        MapId gridMapId = default;
                        foreach (var kvData in data.GridData)
                        {
                            if (kvData.Key != gridEuid)
                                continue;

                            gridMapId = kvData.Value.Coordinates.MapId;
                            break;
                        }

                        DebugTools.Assert(gridMapId != default, $"Could not find corresponding gridData for new grid {gridEuid}.");

                        _newGrids.Add((gridMapId, gridEuid, chunkSize));
                    }
                }
            }

            // create all the new maps
            foreach (var (mapId, euid) in _newMaps)
            {
                CreateMap(mapId, euid);
            }
            _newMaps.Clear();

            // create all the new grids
            foreach (var (mapId, euid, chunkSize) in _newGrids)
            {
                CreateGrid(mapId, chunkSize, euid);
            }
            _newGrids.Clear();
        }

        // Process all grid updates.
        if (data != null && data.GridData != null)
        {
            // Ok good all the grids and maps exist now.
            foreach (var (gridId, gridDatum) in data.GridData)
            {
                var xformComp = EntityManager.GetComponent<TransformComponent>(gridId);
                ApplyTransformState(xformComp, gridDatum);

                var gridComp = EntityManager.GetComponent<MapGridComponent>(gridId);
                MapGridComponent.ApplyMapGridState(this, gridComp, gridDatum.ChunkData);
            }
        }
    }

    private static void ApplyTransformState(TransformComponent xformComp, GameStateMapData.GridDatum gridDatum)
    {
        if (xformComp.MapID != gridDatum.Coordinates.MapId)
            throw new NotImplementedException("Moving grids between maps is not yet implemented");

        // TODO: SHITCODE ALERT -> When we get proper ECS we can delete this.
        if (xformComp.WorldPosition != gridDatum.Coordinates.Position)
        {
            xformComp.WorldPosition = gridDatum.Coordinates.Position;
        }

        if (xformComp.WorldRotation != gridDatum.Angle)
        {
            xformComp.WorldRotation = gridDatum.Angle;
        }
    }
}
