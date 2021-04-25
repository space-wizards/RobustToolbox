using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Robust.Server.Map
{
    internal sealed class ServerMapManager : MapManager, IServerMapManager
    {
        [Dependency] private readonly INetManager _netManager = default!;

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

                var gridDatum =
                    new GameStateMapData.GridDatum(chunkData.ToArray(), new MapCoordinates(grid.WorldPosition, grid.ParentMapId));

                gridDatums.Add(grid.Index, gridDatum);
            }

            var mapDeletionsData = _mapDeletionHistory.Where(d => d.tick >= fromTick).Select(d => d.mapId).ToList();
            var gridDeletionsData = _gridDeletionHistory.Where(d => d.tick >= fromTick).Select(d => d.gridId).ToList();
            var mapCreations = _mapCreationTick.Where(kv => kv.Value >= fromTick && kv.Key != MapId.Nullspace)
                .Select(kv => kv.Key).ToArray();
            var gridCreations = _grids.Values.Where(g => g.CreatedTick >= fromTick && g.ParentMapId != MapId.Nullspace).ToDictionary(g => g.Index,
                grid => new GameStateMapData.GridCreationDatum(grid.ChunkSize, grid.SnapSize));

            // no point sending empty collections
            if (gridDatums.Count        == 0)  gridDatums        = default;
            if (gridDeletionsData.Count == 0)  gridDeletionsData = default;
            if (mapDeletionsData.Count  == 0)  mapDeletionsData  = default;
            if (mapCreations.Length     == 0)  mapCreations      = default;
            if (gridCreations.Count     == 0)  gridCreations     = default;

            // no point even creating an empty map state if no data
            if (gridDatums == null && gridDeletionsData == null && mapDeletionsData == null && mapCreations == null && gridCreations == null)
                return default;

            return new GameStateMapData(gridDatums?.ToArray(), gridDeletionsData?.ToArray(), mapDeletionsData?.ToArray(), mapCreations?.ToArray(), gridCreations?.ToArray());
        }

        public void CullDeletionHistory(GameTick uptoTick)
        {
            _mapDeletionHistory.RemoveAll(t => t.tick < uptoTick);
            _gridDeletionHistory.RemoveAll(t => t.tick < uptoTick);
        }
    }
}
