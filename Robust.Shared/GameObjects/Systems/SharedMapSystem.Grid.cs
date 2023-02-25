using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    #region Chunk helpers

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetChunkIndices(Vector2 tile, int chunkSize)
    {
        return new Vector2i ((int) Math.Floor(tile.X / chunkSize), (int) Math.Floor(tile.Y / chunkSize));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetChunkIndices(Vector2 tile, byte chunkSize)
    {
        return new Vector2i ((int) Math.Floor(tile.X / chunkSize), (int) Math.Floor(tile.Y / chunkSize));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetChunkIndices(Vector2i tile, int chunkSize)
    {
        return new Vector2i ((int) Math.Floor(tile.X / (float) chunkSize), (int) Math.Floor(tile.Y / (float) chunkSize));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetChunkIndices(Vector2i tile, byte chunkSize)
    {
        return new Vector2i ((int) Math.Floor(tile.X / (float) chunkSize), (int) Math.Floor(tile.Y / (float) chunkSize));
    }

    /// <summary>
    /// Returns the tile offset to a chunk origin based on the provided size.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetChunkRelative(Vector2 tile, int chunkSize)
    {
        var x = MathHelper.Mod((int) Math.Floor(tile.X), chunkSize);
        var y = MathHelper.Mod((int) Math.Floor(tile.Y), chunkSize);
        return new Vector2i(x, y);
    }

    /// <summary>
    /// Returns the tile offset to a chunk origin based on the provided size.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetChunkRelative(Vector2 tile, byte chunkSize)
    {
        var x = MathHelper.Mod((int) Math.Floor(tile.X), chunkSize);
        var y = MathHelper.Mod((int) Math.Floor(tile.Y), chunkSize);
        return new Vector2i(x, y);
    }

    /// <summary>
    /// Returns the tile offset to a chunk origin based on the provided size.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetChunkRelative(Vector2i tile, int chunkSize)
    {
        var x = MathHelper.Mod(tile.X, chunkSize);
        var y = MathHelper.Mod(tile.Y, chunkSize);
        return new Vector2i(x, y);
    }

    /// <summary>
    /// Returns the tile offset to a chunk origin based on the provided size.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetChunkRelative(Vector2i tile, byte chunkSize)
    {
        var x = MathHelper.Mod(tile.X, chunkSize);
        var y = MathHelper.Mod(tile.Y, chunkSize);
        return new Vector2i(x, y);
    }

    #endregion

    public static Vector2i GetDirection(Vector2i position, Direction dir, int dist = 1)
    {
        switch (dir)
        {
            case Direction.East:
                return position + new Vector2i(dist, 0);
            case Direction.SouthEast:
                return position + new Vector2i(dist, -dist);
            case Direction.South:
                return position + new Vector2i(0, -dist);
            case Direction.SouthWest:
                return position + new Vector2i(-dist, -dist);
            case Direction.West:
                return position + new Vector2i(-dist, 0);
            case Direction.NorthWest:
                return position + new Vector2i(-dist, dist);
            case Direction.North:
                return position + new Vector2i(0, dist);
            case Direction.NorthEast:
                return position + new Vector2i(dist, dist);
            default:
                throw new NotImplementedException();
        }
    }

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

        if (state.ChunkData == null && state.FullGridData == null)
            return;

        var modified = new List<(Vector2i position, Tile tile)>();
        MapManager.SuppressOnTileChanged = true;

        // delta state
        if (state.ChunkData != null)
        {
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
        }

        // full state
        if (state.FullGridData != null)
        {
            foreach (var index in component.Chunks.Keys)
            {
                if (!state.FullGridData.ContainsKey(index))
                    component.RemoveChunk(index);
            }

            foreach (var (index, tiles) in state.FullGridData)
            {
                var chunk = component.GetOrAddChunk(index);
                chunk.SuppressCollisionRegeneration = true;
                DebugTools.Assert(tiles.Length == component.ChunkSize * component.ChunkSize);

                var counter = 0;
                for (ushort x = 0; x < component.ChunkSize; x++)
                {
                    for (ushort y = 0; y < component.ChunkSize; y++)
                    {
                        var tile = tiles[counter++];
                        if (chunk.GetTile(x, y) == tile)
                            continue;

                        chunk.SetTile(x, y, tile);
                        modified.Add((new Vector2i(chunk.X * component.ChunkSize + x, chunk.Y * component.ChunkSize + y), tile));
                    }
                }

                chunk.SuppressCollisionRegeneration = false;
                component.RegenerateCollision(chunk);
            }
        }

        MapManager.SuppressOnTileChanged = false;
        if (modified.Count != 0)
            RaiseLocalEvent(uid, new GridModifiedEvent(component, modified), true);
    }

    private void OnGridGetState(EntityUid uid, MapGridComponent component, ref ComponentGetState args)
    {
        if (args.FromTick <= component.CreationTick)
        {
            GetFullState(component, ref args);
            return;
        }

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
                if (tick < fromTick && fromTick != GameTick.Zero)
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

        args.State = new MapGridComponentState(component.ChunkSize, chunkData);
    }

    private void GetFullState(MapGridComponent component, ref ComponentGetState args)
    {
        var chunkData = new Dictionary<Vector2i, Tile[]>();

        foreach (var (index, chunk) in component.GetMapChunks())
        {
            var tileBuffer = new Tile[component.ChunkSize * (uint)component.ChunkSize];

            for (var x = 0; x < component.ChunkSize; x++)
            {
                for (var y = 0; y < component.ChunkSize; y++)
                {
                    tileBuffer[x * component.ChunkSize + y] = chunk.GetTile((ushort)x, (ushort)y);
                }
            }
            chunkData.Add(index, tileBuffer);
        }

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
                _transform.SetParent(uid, xform, MapManager.GetMapEntityIdOrThrow(mapId), xformQuery);
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
