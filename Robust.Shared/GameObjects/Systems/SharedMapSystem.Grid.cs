using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedMapSystem
{
    private void InitializeGrid()
    {
        SubscribeLocalEvent<MapGridComponent, ComponentGetState>(OnGridGetState);
        SubscribeLocalEvent<MapGridComponent, ComponentHandleState>(OnGridHandleState);
        SubscribeLocalEvent<MapGridComponent, ComponentAdd>(OnGridAdd);
        SubscribeLocalEvent<MapGridComponent, ComponentInit>(OnGridInit);
        SubscribeLocalEvent<MapGridComponent, ComponentStartup>(OnGridStartup);
        SubscribeLocalEvent<MapGridComponent, ComponentShutdown>(OnGridRemove);
    }

    private void OnGridHandleState(EntityUid uid, MapGridComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MapGridComponentState state)
            return;

        component.ChunkSize = state.ChunkSize;

        if (state.ChunkData != null)
        {
            var modified = new List<(Vector2i position, Tile tile)>();
            MapManager.SuppressOnTileChanged = true;
            foreach (var chunkData in state.ChunkData)
            {
                if (chunkData.IsDeleted())
                    continue;

                var chunk = component.GetOrAddChunk(chunkData.Index);
                chunk.SuppressCollisionRegeneration = true;
                DebugTools.Assert(chunkData.TileData.Length == component.ChunkSize * component.ChunkSize);

                var counter = 0;
                for (ushort x = 0; x < component.ChunkSize; x++)
                {
                    for (ushort y = 0; y < component.ChunkSize; y++)
                    {
                        var tile = chunkData.TileData[counter++];
                        if (chunk.GetTile(x, y) == tile)
                            continue;

                        chunk.SetTile(x, y, tile);
                        modified.Add((new Vector2i(chunk.X * component.ChunkSize + x, chunk.Y * component.ChunkSize + y), tile));
                    }
                }
            }

            foreach (var chunkData in state.ChunkData)
            {
                if (chunkData.IsDeleted())
                {
                    component.RemoveChunk(chunkData.Index);
                    continue;
                }

                var chunk = component.GetOrAddChunk(chunkData.Index);
                chunk.SuppressCollisionRegeneration = false;
                component.RegenerateCollision(chunk);
            }

            MapManager.SuppressOnTileChanged = false;

            if (modified.Count != 0)
            {
                RaiseLocalEvent(uid, new GridModifiedEvent(component, modified), true);
            }
        }
    }

    private void OnGridGetState(EntityUid uid, MapGridComponent component, ref ComponentGetState args)
    {
        // TODO: Actual deltas.
        List<ChunkDatum>? chunkData;
        var fromTick = args.FromTick;

        if (component.LastTileModifiedTick < fromTick)
        {
            chunkData = null;
        }
        else
        {
            chunkData = new List<ChunkDatum>();
            var chunks = component.ChunkDeletionHistory;

            foreach (var (tick, indices) in chunks)
            {
                if (tick < fromTick)
                    continue;

                chunkData.Add(ChunkDatum.CreateDeleted(indices));
            }

            foreach (var (index, chunk) in component.GetMapChunks())
            {
                if (chunk.LastTileModifiedTick < fromTick)
                    continue;

                var tileBuffer = new Tile[component.ChunkSize * (uint) component.ChunkSize];

                // Flatten the tile array.
                // NetSerializer doesn't do multi-dimensional arrays.
                // This is probably really expensive.
                for (var x = 0; x < component.ChunkSize; x++)
                {
                    for (var y = 0; y < component.ChunkSize; y++)
                    {
                        tileBuffer[x * component.ChunkSize + y] = chunk.GetTile((ushort)x, (ushort)y);
                    }
                }
                chunkData.Add(ChunkDatum.CreateModified(index, tileBuffer));
            }
        }

        // TODO: Mark it as delta proper
        args.State = new MapGridComponentState(component.ChunkSize, chunkData);
    }

    private void OnGridAdd(EntityUid uid, MapGridComponent component, ComponentAdd args)
    {
        // GridID is not set yet so we don't include it.
        var msg = new GridAddEvent(uid);
        RaiseLocalEvent(uid, msg, true);
    }

    private void OnGridInit(EntityUid uid, MapGridComponent component, ComponentInit args)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(uid);
        var mapId = xform.MapID;

        if (MapManager.HasMapEntity(mapId))
        {
            var mapUid = MapManager.GetMapEntityIdOrThrow(mapId);

            // Mapgrid moment
            if (mapUid != uid)
                _transform.SetParent(xform, MapManager.GetMapEntityIdOrThrow(mapId), xformQuery);
        }

        // Force networkedmapmanager to send it due to non-ECS legacy code.
        var curTick = _timing.CurTick;

        foreach (var chunk in component.Chunks.Values)
        {
            chunk.TileModified += component.OnTileModified;
            chunk.LastTileModifiedTick = curTick;
        }

        component.LastTileModifiedTick = curTick;

        // Just in case.
        _transform.SetGridId(xform, uid, xformQuery);

        var msg = new GridInitializeEvent(uid);
        RaiseLocalEvent(uid, msg, true);
    }

    private void OnGridStartup(EntityUid uid, MapGridComponent component, ComponentStartup args)
    {
        var msg = new GridStartupEvent(uid);
        RaiseLocalEvent(uid, msg, true);
    }

    private void OnGridRemove(EntityUid uid, MapGridComponent component, ComponentShutdown args)
    {
        RaiseLocalEvent(uid, new GridRemovalEvent(uid), true);

        if (uid == EntityUid.Invalid)
            return;

        if (!MapManager.GridExists(uid))
            return;

        MapManager.DeleteGrid(uid);
    }
}
