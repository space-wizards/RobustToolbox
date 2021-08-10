using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Server.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.Map
{
    [UsedImplicitly]
    internal sealed class ServerMapManager : MapManager, IServerMapManager
    {
        private readonly Dictionary<GridId, List<(GameTick tick, Vector2i indices)>> _chunkDeletionHistory = new();
        private readonly List<(GameTick tick, GridId gridId)> _gridDeletionHistory = new();
        private readonly List<(GameTick tick, MapId mapId)> _mapDeletionHistory = new();

        public override void DeleteMap(MapId mapID)
        {
            base.DeleteMap(mapID);
            _mapDeletionHistory.Add((GameTiming.CurTick, mapID));
        }

        public override void DeleteGrid(GridId gridID)
        {
            base.DeleteGrid(gridID);
            _gridDeletionHistory.Add((GameTiming.CurTick, gridID));
            // No point syncing chunk removals anymore!
            _chunkDeletionHistory.Remove(gridID);
        }

        public override void ChunkRemoved(MapChunk chunk)
        {
            base.ChunkRemoved(chunk);
            if (!_chunkDeletionHistory.TryGetValue(chunk.GridId, out var chunks))
            {
                chunks = new List<(GameTick tick, Vector2i indices)>();
                _chunkDeletionHistory[chunk.GridId] = chunks;
            }

            EntitySystem.Get<GridFixtureSystem>().RemoveChunk(chunk);
            chunks.Add((GameTiming.CurTick, chunk.Indices));
        }

        public GameStateMapData? GetStateData(GameTick fromTick)
        {
            var gridDatums = new Dictionary<GridId, GameStateMapData.GridDatum>();
            foreach (var grid in _grids.Values)
            {
                if (grid.LastModifiedTick < fromTick)
                {
                    continue;
                }

                var deletedChunkData = new List<GameStateMapData.DeletedChunkDatum>();

                if (_chunkDeletionHistory.TryGetValue(grid.Index, out var chunks))
                {
                    foreach (var (tick, indices) in chunks)
                    {
                        if (tick < fromTick) continue;

                        deletedChunkData.Add(new GameStateMapData.DeletedChunkDatum(indices));
                    }
                }

                var chunkData = new List<GameStateMapData.ChunkDatum>();

                foreach (var (index, chunk) in grid.GetMapChunks())
                {
                    if (chunk.LastModifiedTick < fromTick)
                    {
                        continue;
                    }

                    var tileBuffer = new Tile[grid.ChunkSize * (uint) grid.ChunkSize];

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
                        new MapCoordinates(grid.WorldPosition, grid.ParentMapId));

                gridDatums.Add(grid.Index, gridDatum);
            }

            var mapDeletionsData = _mapDeletionHistory.Where(d => d.tick >= fromTick).Select(d => d.mapId).ToList();
            var gridDeletionsData = _gridDeletionHistory.Where(d => d.tick >= fromTick).Select(d => d.gridId).ToList();
            var mapCreations = _mapCreationTick.Where(kv => kv.Value >= fromTick && kv.Key != MapId.Nullspace)
                .Select(kv => kv.Key).ToArray();
            var gridCreations = _grids.Values.Where(g => g.CreatedTick >= fromTick && g.ParentMapId != MapId.Nullspace).ToDictionary(g => g.Index,
                grid => new GameStateMapData.GridCreationDatum(grid.ChunkSize));

            // no point sending empty collections
            if (gridDatums.Count        == 0)  gridDatums        = default;
            if (gridDeletionsData.Count == 0)  gridDeletionsData = default;
            if (mapDeletionsData.Count  == 0)  mapDeletionsData  = default;
            if (mapCreations.Length     == 0)  mapCreations      = default;
            if (gridCreations.Count     == 0)  gridCreations     = default;

            // no point even creating an empty map state if no data
            if (gridDatums == null && gridDeletionsData == null && mapDeletionsData == null && mapCreations == null && gridCreations == null)
                return default;

            return new GameStateMapData(gridDatums?.ToArray<KeyValuePair<GridId, GameStateMapData.GridDatum>>(), gridDeletionsData?.ToArray(), mapDeletionsData?.ToArray(), mapCreations?.ToArray(), gridCreations?.ToArray<KeyValuePair<GridId, GameStateMapData.GridCreationDatum>>());
        }

        public void CullDeletionHistory(GameTick uptoTick)
        {
            foreach (var (gridId, chunks) in _chunkDeletionHistory.ToArray())
            {
                chunks.RemoveAll(t => t.tick < uptoTick);
                if (chunks.Count == 0) _chunkDeletionHistory.Remove(gridId);
            }

            _mapDeletionHistory.RemoveAll(t => t.tick < uptoTick);
            _gridDeletionHistory.RemoveAll(t => t.tick < uptoTick);
        }
    }
}
