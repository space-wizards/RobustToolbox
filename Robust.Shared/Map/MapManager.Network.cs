using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Map
{
    internal partial class MapManager
    {
        [Dependency] private readonly INetManager _netManager = default!;

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

        public void ApplyGameStatePre(GameStateMapData? data)
        {
            DebugTools.Assert(_netManager.IsClient, "Only the client should call this.");

            // There was no map data this tick, so nothing to do.
            if(data == null)
                return;

            var createdGrids = data.CreatedGrids != null
                ? new Dictionary<GridId, GameStateMapData.GridCreationDatum>(data.CreatedGrids)
                : null;

            // First we need to figure out all the NEW MAPS.
            if(data.CreatedMaps != null)
            {
                foreach (var mapId in data.CreatedMaps)
                {
                    if (_maps.Contains(mapId))
                    {
                        continue;
                    }

                    CreateMap(mapId);
                }
            }

            // Then make all the grids.
            if(data.CreatedGrids != null)
            {
                var gridData = data.GridData != null
                    ? new Dictionary<GridId, GameStateMapData.GridDatum>(data.GridData)
                    : null;

                DebugTools.AssertNotNull(createdGrids);

                foreach (var (gridId, creationDatum) in data.CreatedGrids)
                {
                    if (_grids.ContainsKey(gridId))
                    {
                        continue;
                    }

                    CreateGrid(gridData![gridId].Coordinates.MapId, gridId, creationDatum.ChunkSize,
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
                    if (grid.ParentMapId != gridDatum.Coordinates.MapId)
                    {
                        throw new NotImplementedException("Moving grids between maps is not yet implemented");
                    }

                    grid.WorldPosition = gridDatum.Coordinates.Position;

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
                                if (chunk.GetTileRef(x, y).Tile != tile)
                                {
                                    chunk.SetTile(x, y, tile);
                                    modified.Add((new Vector2i(chunk.X * grid.ChunkSize + x, chunk.Y * grid.ChunkSize + y), tile));
                                }
                            }
                        }

                        chunk.SuppressCollisionRegeneration = false;
                        chunk.RegenerateCollision();
                    }

                    if (modified.Count != 0)
                    {
                        GridChanged?.Invoke(this, new GridChangedEventArgs(grid, modified));
                    }
                }

                SuppressOnTileChanged = false;
            }
        }

        public void ApplyGameStatePost(GameStateMapData? data)
        {
            DebugTools.Assert(_netManager.IsClient, "Only the client should call this.");

            if(data == null) // if there is no data, there is nothing to do!
                return;

            // maps created on the client in pre-state are linked to client entities
            // resolve new maps with their shared component that the server just gave us
            // and delete the client entities
            if (data.CreatedMaps != null)
            {
                foreach (var mapId in data.CreatedMaps)
                {
                    // CreateMap should have set this
                    DebugTools.Assert(_mapEntities.ContainsKey(mapId));

                    // this was already linked in a previous state.
                    if(!_mapEntities[mapId].IsClientSide())
                        continue;

                    // get the existing client entity for the map.
                    var cEntity = _entityManager.GetEntity(_mapEntities[mapId]);

                    // locate the entity that represents this map that was just sent to us
                    IEntity? sharedMapEntity = null;
                    var mapComps = _entityManager.ComponentManager.EntityQuery<IMapComponent>(true);
                    foreach (var mapComp in mapComps)
                    {
                        if (!mapComp.Owner.Uid.IsClientSide() && mapComp.WorldMap == mapId)
                        {
                            sharedMapEntity = mapComp.Owner;
                            _mapEntities[mapId] = mapComp.Owner.Uid;
                            Logger.DebugS("map", $"Map {mapId} pivoted bound entity from {cEntity.Uid} to {mapComp.Owner.Uid}.");
                            break;
                        }
                    }

                    // verify shared entity was found (the server sent us one)
                    DebugTools.AssertNotNull(sharedMapEntity);
                    DebugTools.Assert(!_mapEntities[mapId].IsClientSide());

                    // Transfer client child grids made in GameStatePre to the shared component
                    // so they are not deleted
                    foreach (var childGridTrans in cEntity.Transform.Children.ToList())
                    {
                        childGridTrans.AttachParent(sharedMapEntity!);
                    }

                    // remove client entity
                    var cGridComp = cEntity.GetComponent<IMapComponent>();
                    cGridComp.ClearMapId();
                    cEntity.Delete();
                }
            }


            // grids created on the client in pre-state are linked to client entities
            // resolve new grids with their shared component that the server just gave us
            // and delete the client entities
            if (data.CreatedGrids != null)
            {
                foreach (var kvNewGrid in data.CreatedGrids)
                {
                    var grid = _grids[kvNewGrid.Key];

                    // this was already linked in a previous state.
                    if(!grid.GridEntityId.IsClientSide())
                        continue;

                    // remove the existing client entity.
                    var cEntity = _entityManager.GetEntity(grid.GridEntityId);
                    var cGridComp = cEntity.GetComponent<IMapGridComponent>();

                    // prevents us from deleting the grid when deleting the grid entity
                    if(cEntity.Uid.IsClientSide())
                        cGridComp.ClearGridId();

                    cEntity.Delete(); // normal entities are already parented to the shared comp, client comp has no children

                    var gridComps = _entityManager.ComponentManager.EntityQuery<IMapGridComponent>(true);
                    foreach (var gridComp in gridComps)
                    {
                        if (gridComp.GridIndex == kvNewGrid.Key)
                        {
                            grid.GridEntityId = gridComp.Owner.Uid;
                            Logger.DebugS("map", $"Grid {grid.Index} pivoted bound entity from {cEntity.Uid} to {grid.GridEntityId}.");
                            break;
                        }
                    }

                    DebugTools.Assert(!grid.GridEntityId.IsClientSide());
                }
            }

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
                    if (_maps.Contains(map))
                    {
                        DeleteMap(map);
                    }
                }
            }
        }
    }
}
