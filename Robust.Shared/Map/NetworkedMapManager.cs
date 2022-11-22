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
        var query = EntityManager.AllEntityQueryEnumerator<MapGridComponent>();

        while (query.MoveNext(out var grid))
        {
            var chunks = grid.ChunkDeletionHistory;
            chunks.RemoveAll(t => t.tick < upToTick);
        }
    }
}
