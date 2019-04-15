using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network.Messages;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Map
{
    public partial class MapManager
    {
        [Dependency] private readonly INetManager _netManager;

        public GameStateMapData GetStateData(GameTick fromTick)
        {
            var gridDatums = new Dictionary<GridId, GameStateMapData.GridDatum>();
            foreach (var grid in _grids.Values)
            {
                if (grid.LastModifiedTick < fromTick)
                {
                    continue;
                }

                var chunkData = new List<GameStateMapData.ChunkDatum>();
                foreach (var (index, chunk) in grid._chunks)
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
                    for (var y = 0; y < grid.ChunkSize; y++)
                    {
                        tileBuffer[x * grid.ChunkSize + y] = chunk._tiles[x, y];
                    }

                    chunkData.Add(new GameStateMapData.ChunkDatum(index, tileBuffer));
                }

                var gridDatum =
                    new GameStateMapData.GridDatum(chunkData, new MapCoordinates(grid.WorldPosition, grid.MapID));

                gridDatums.Add(grid.Index, gridDatum);
            }

            var mapDeletionsData = _mapDeletionHistory.Where(d => d.tick >= fromTick).Select(d => d.mapId).ToList();
            var gridDeletionsData = _gridDeletionHistory.Where(d => d.tick >= fromTick).Select(d => d.gridId).ToList();
            var mapCreations = _maps.Values.Where(m => m.CreatedTick >= fromTick)
                .ToDictionary(m => m.Index, m => m.DefaultGrid.Index);
            var gridCreations = _grids.Values.Where(g => g.CreatedTick >= fromTick).ToDictionary(g => g.Index,
                grid => new GameStateMapData.GridCreationDatum(grid.ChunkSize, grid.SnapSize,
                    grid.IsDefaultGrid));

            // no point sending empty collections
            if (gridDatums.Count        == 0) gridDatums        = default;
            if (gridDeletionsData.Count == 0) gridDeletionsData = default;
            if (mapDeletionsData.Count  == 0) mapDeletionsData  = default;
            if (mapCreations.Count      == 0) mapCreations      = default;
            if (gridCreations.Count     == 0) gridCreations     = default;

            // no point even creating an empty map state if no data
            if (gridDatums == null && gridDeletionsData == null && mapDeletionsData == null && mapCreations == null && gridCreations == null)
                return default;

            return new GameStateMapData(gridDatums, gridDeletionsData, mapDeletionsData, mapCreations, gridCreations);
        }

        public void CullDeletionHistory(GameTick uptoTick)
        {
            _mapDeletionHistory.RemoveAll(t => t.tick < uptoTick);
            _gridDeletionHistory.RemoveAll(t => t.tick < uptoTick);
        }

        public void ApplyGameStatePre(GameStateMapData data)
        {
            DebugTools.Assert(_netManager.IsClient, "Only the client should call this.");

            // There was no map data this tick, so nothing to do.
            if(data == null)
                return;

            // First we need to figure out all the NEW MAPS.
            // And make their default grids too.
            if(data.CreatedMaps != null)
            {
                foreach (var (mapId, gridId) in data.CreatedMaps)
                {
                    if (_maps.ContainsKey(mapId))
                    {
                        continue;
                    }
                    var gridCreation = data.CreatedGrids[gridId];
                    DebugTools.Assert(gridCreation.IsTheDefault);

                    var newMap = new Map(this, mapId);
                    _maps.Add(mapId, newMap);
                    MapCreated?.Invoke(this, new MapEventArgs(newMap));
                    newMap.DefaultGrid = CreateGrid(newMap.Index, gridId, gridCreation.ChunkSize, gridCreation.SnapSize);
                }
            }

            // Then make all the other grids.
            if(data.CreatedGrids != null)
            {
                foreach (var (gridId, creationDatum) in data.CreatedGrids)
                {
                    if (creationDatum.IsTheDefault || _grids.ContainsKey(gridId))
                    {
                        continue;
                    }

                    CreateGrid(data.GridData[gridId].Coordinates.MapId, gridId, creationDatum.ChunkSize,
                        creationDatum.SnapSize);
                }
            }

            if(data.GridData != null)
            {
                SuppressOnTileChanged = true;
                // Ok good all the grids and maps exist now.
                foreach (var (gridId, gridDatum) in data.GridData)
                {
                    var grid = _grids[gridId];
                    if (grid.MapID != gridDatum.Coordinates.MapId)
                    {
                        throw new NotImplementedException("Moving grids between maps is not yet implemented");
                    }

                    grid.WorldPosition = gridDatum.Coordinates.Position;

                    var modified = new List<(MapIndices position, Tile tile)>();
                    foreach (var chunkData in gridDatum.ChunkData)
                    {
                        var chunk = grid.GetChunk(chunkData.Index);
                        DebugTools.Assert(chunkData.TileData.Length == grid.ChunkSize * grid.ChunkSize);

                        var counter = 0;
                        for (ushort x = 0; x < grid.ChunkSize; x++)
                        for (ushort y = 0; y < grid.ChunkSize; y++)
                        {
                            var tile = chunkData.TileData[counter++];
                            if (chunk.GetTile(x, y).Tile != tile)
                            {
                                chunk.SetTile(x, y, tile);
                                modified.Add((new MapIndices(chunk.X * grid.ChunkSize + x, chunk.Y * grid.ChunkSize + y), tile));
                            }
                        }
                    }

                    if (modified.Count != 0)
                    {
                        GridChanged?.Invoke(this, new GridChangedEventArgs(grid, modified));
                    }
                }

                SuppressOnTileChanged = false;
            }
        }

        public void ApplyGameStatePost(GameStateMapData data)
        {
            DebugTools.Assert(_netManager.IsClient, "Only the client should call this.");

            if(data == null) // if there is no data, there is nothing to do!
                return;

            if(data.DeletedGrids != null)
            {
                foreach (var grid in data.DeletedGrids)
                {
                    if (_grids.ContainsKey(grid))
                    {
                        DeleteGrid(grid);
                    }
                }
            }

            if(data.DeletedMaps != null)
            {
                foreach (var map in data.DeletedMaps)
                {
                    if (_maps.ContainsKey(map))
                    {
                        DeleteMap(map);
                    }
                }
            }
        }
    }
}
