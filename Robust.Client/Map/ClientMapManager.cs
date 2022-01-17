using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Map
{
    internal class ClientMapManager : MapManager, IClientMapManager
    {
        public void ApplyGameStatePre(GameStateMapData? data, ReadOnlySpan<EntityState> entityStates)
        {
            // There was no map data this tick, so nothing to do.
            if(data == null)
                return;

            // First we need to figure out all the NEW MAPS.
            if(data.CreatedMaps != null)
            {
                DebugTools.Assert(!entityStates.IsEmpty, "Received new maps, but no entity state.");

                foreach (var mapId in data.CreatedMaps)
                {
                    // map already exists from a previous state.
                    if (_maps.Contains(mapId))
                        continue;

                    EntityUid mapEuid = default;

                    //get shared euid of map comp entity
                    foreach (var entityState in entityStates!)
                    {
                        foreach (var compChange in entityState.ComponentChanges.Span)
                        {
                            if (compChange.State is not MapComponentState mapCompState || mapCompState.MapId != mapId)
                                continue;

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
            if(data.CreatedGrids != null)
            {
                DebugTools.Assert(data.GridData is not null, "Received new grids, but GridData was null.");

                foreach (var (gridId, creationDatum) in data.CreatedGrids)
                {
                    if (_grids.ContainsKey(gridId))
                        continue;

                    EntityUid gridEuid = default;

                    //get shared euid of map comp entity
                    foreach (var entityState in entityStates!)
                    {
                        foreach (var compState in entityState.ComponentChanges.Span)
                        {
                            if (compState.State is not MapGridComponentState gridCompState || gridCompState.GridIndex != gridId)
                                continue;

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
                                if (chunk.GetTileRef(x, y).Tile != tile)
                                {
                                    chunk.SetTile(x, y, tile);
                                    modified.Add((new Vector2i(chunk.X * grid.ChunkSize + x, chunk.Y * grid.ChunkSize + y), tile));
                                }
                            }
                        }
                    }

                    if (modified.Count != 0)
                    {
                        InvokeGridChanged(this, new GridChangedEventArgs(grid, modified));
                    }

                    foreach (var chunkData in gridDatum.ChunkData)
                    {
                        var chunk = grid.GetChunk(chunkData.Index);
                        chunk.SuppressCollisionRegeneration = false;
                        chunk.RegenerateCollision();
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
                    if (_maps.Contains(map))
                    {
                        DeleteMap(map);
                    }
                }
            }
        }
    }
}
