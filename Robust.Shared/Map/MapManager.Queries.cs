using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Robust.Shared.Map;

internal partial class MapManager
{
    [Obsolete("Use the FindGridsIntersecting callback")]
    public IEnumerable<MapGridComponent> FindGridsIntersecting(MapId mapId, Box2Rotated bounds, bool approx = false, bool includeMap = true)
    {
        var aabb = bounds.CalcBoundingBox();
        // TODO: We can do slower GJK checks to check if 2 bounds actually intersect, but WYCI.
        return FindGridsIntersecting(mapId, aabb, includeMap, approx);
    }

    public void FindGridsIntersecting(MapId mapId, Box2 worldAABB, GridCallback callback, bool approx = false, bool includeMap = true)
    {
        if (!_mapEntities.TryGetValue(mapId, out var mapEnt) ||
            !EntityManager.TryGetComponent<GridTreeComponent>(mapEnt, out var gridTree))
        {
            return;
        }

        var physicsQuery = EntityManager.GetEntityQuery<PhysicsComponent>();
        var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();
        var xformSystem = EntityManager.System<SharedTransformSystem>();
        var state = (worldAABB, gridTree.Tree, callback, approx, physicsQuery, xformQuery, xformSystem);

        gridTree.Tree.Query(ref state,
            static (ref (Box2 worldAABB,
                    B2DynamicTree<(EntityUid Uid, MapGridComponent Grid)> gridTree,
                    GridCallback callback,
                    bool approx,
                    EntityQuery<PhysicsComponent> physicsQuery, EntityQuery<TransformComponent> xformQuery,
                    SharedTransformSystem xformSystem) tuple,
                DynamicTree.Proxy proxy) =>
            {
                var data = tuple.gridTree.GetUserData(proxy);

                if (!tuple.approx && !IsIntersecting(tuple.worldAABB, data.Uid, data.Grid,
                        tuple.physicsQuery, tuple.xformQuery, tuple.xformSystem))
                {
                    return true;
                }

                return tuple.callback(data.Uid, data.Grid);
            }, worldAABB);

        var mapUid = GetMapEntityId(mapId);

        if (includeMap && EntityManager.TryGetComponent<MapGridComponent>(mapUid, out var grid))
        {
            callback(mapUid, grid);
        }
    }

    public void FindGridsIntersecting<TState>(MapId mapId, Box2 worldAABB, ref TState state, GridCallback<TState> callback, bool approx = false, bool includeMap = true)
    {
        if (!_mapEntities.TryGetValue(mapId, out var mapEnt) ||
            !EntityManager.TryGetComponent<GridTreeComponent>(mapEnt, out var gridTree))
        {
            return;
        }
        var physicsQuery = EntityManager.GetEntityQuery<PhysicsComponent>();
        var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();
        var xformSystem = EntityManager.System<SharedTransformSystem>();
        var state2 = (state, worldAABB, gridTree.Tree, callback, approx, physicsQuery, xformQuery, xformSystem);

        gridTree.Tree.Query(ref state2, static (ref (
                TState state,
                Box2 worldAABB,
                B2DynamicTree<(EntityUid Uid, MapGridComponent Grid)> gridTree,
                GridCallback<TState> callback,
                bool approx,
                EntityQuery<PhysicsComponent> physicsQuery,
                EntityQuery<TransformComponent> xformQuery,
                SharedTransformSystem xformSystem) tuple,
            DynamicTree.Proxy proxy) =>
        {
            var data = tuple.gridTree.GetUserData(proxy);

            if (!tuple.approx && !IsIntersecting(tuple.worldAABB, data.Uid, data.Grid,
                    tuple.physicsQuery, tuple.xformQuery, tuple.xformSystem))
            {
                return true;
            }

            return tuple.callback(data.Uid, data.Grid, ref tuple.state);
        }, worldAABB);

        var mapUid = GetMapEntityId(mapId);

        if (includeMap && EntityManager.TryGetComponent<MapGridComponent>(mapUid, out var grid))
        {
            callback(mapUid, grid, ref state);
        }

        state = state2.state;
    }

    private static bool IsIntersecting(
        Box2 aabb,
        EntityUid gridUid,
        MapGridComponent grid,
        EntityQuery<PhysicsComponent> physicsQuery,
        EntityQuery<TransformComponent> xformQuery,
        SharedTransformSystem xformSystem)
    {
        var xformComp = xformQuery.GetComponent(gridUid);
        var (worldPos, worldRot, matrix, invMatrix) = xformSystem.GetWorldPositionRotationMatrixWithInv(xformComp, xformQuery);
        var overlap = matrix.TransformBox(grid.LocalAABB).Intersect(aabb);
        var localAABB = invMatrix.TransformBox(overlap);

        if (physicsQuery.HasComponent(gridUid))
        {
            var enumerator = grid.GetLocalMapChunks(localAABB);

            var transform = new Transform(worldPos, worldRot);

            while (enumerator.MoveNext(out var chunk))
            {
                foreach (var fixture in chunk.Fixtures)
                {
                    for (var j = 0; j < fixture.Shape.ChildCount; j++)
                    {
                        // TODO: Should do shape intersects given this is supposed to be non-approx.
                        if (!fixture.Shape.ComputeAABB(transform, j).Intersects(aabb)) continue;

                        return true;
                    }
                }
            }
        }

        return grid.ChunkCount == 0 && aabb.Contains(worldPos);
    }

    [Obsolete("Use the FindGridsIntersecting callback")]
    public IEnumerable<MapGridComponent> FindGridsIntersecting(MapId mapId, Box2 worldAabb, bool approx = false, bool includeMap = true)
    {
        var grids = new List<MapGridComponent>();
        var state = grids;

        FindGridsIntersecting(mapId, worldAabb, ref state,
            (EntityUid _, MapGridComponent grid, ref List<MapGridComponent> list) =>
            {
                list.Add(grid);
                return true;
            }, approx);

        return grids;
    }

    public bool TryFindGridAt(
        MapId mapId,
        Vector2 worldPos,
        EntityQuery<TransformComponent> xformQuery,
        out EntityUid uid,
        [NotNullWhen(true)] out MapGridComponent? grid)
    {
        // Need to enlarge the AABB by at least the grid shrinkage size.
        var aabb = new Box2(worldPos - 0.2f, worldPos + 0.2f);

        uid = EntityUid.Invalid;
        grid = null;
        var xformSystem = EntityManager.System<SharedTransformSystem>();
        var state = (uid, grid, worldPos, xformQuery, xformSystem);

        FindGridsIntersecting(mapId, aabb, ref state, static (EntityUid iUid, MapGridComponent iGrid, ref (
            EntityUid uid,
            MapGridComponent? grid,
            Vector2 worldPos,
            EntityQuery<TransformComponent> xformQuery,
            SharedTransformSystem xformSystem) tuple) =>
        {
            // Turn the worldPos into a localPos and work out the relevant chunk we need to check
            // This is much faster than iterating over every chunk individually.
            // (though now we need some extra calcs up front).

            // Doesn't use WorldBounds because it's just an AABB.
            var matrix = tuple.xformSystem.GetInvWorldMatrix(iUid, tuple.xformQuery);
            var localPos = matrix.Transform(tuple.worldPos);

            // NOTE:
            // If you change this to use fixtures instead (i.e. if you want half-tiles) then you need to make sure
            // you account for the fact that fixtures are shrunk slightly!
            var chunkIndices = SharedMapSystem.GetChunkIndices(localPos, iGrid.ChunkSize);

            if (!iGrid.HasChunk(chunkIndices)) return true;

            var chunk = iGrid.GetOrAddChunk(chunkIndices);
            var chunkRelative = SharedMapSystem.GetChunkRelative(localPos, iGrid.ChunkSize);
            var chunkTile = chunk.GetTile(chunkRelative);

            if (chunkTile.IsEmpty) return true;

            tuple.uid = iUid;
            tuple.grid = iGrid;
            return false;
        }, approx: true);

        var mapUid = GetMapEntityId(mapId);

        if (state.grid == null && EntityManager.TryGetComponent<MapGridComponent>(mapUid, out var mapGrid))
        {
            uid = mapUid;
            grid = mapGrid;
            return true;
        }

        uid = state.uid;
        grid = state.grid;
        return grid != null;
    }

    /// <summary>
    /// Attempts to find the map grid under the map location.
    /// </summary>
    public bool TryFindGridAt(MapId mapId, Vector2 worldPos, out EntityUid uid, [NotNullWhen(true)] out MapGridComponent? grid)
    {
        var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();
        return TryFindGridAt(mapId, worldPos, xformQuery, out uid, out grid);
    }

    /// <summary>
    /// Attempts to find the map grid under the map location.
    /// </summary>
    public bool TryFindGridAt(MapCoordinates mapCoordinates, out EntityUid uid, [NotNullWhen(true)] out MapGridComponent? grid)
    {
        return TryFindGridAt(mapCoordinates.MapId, mapCoordinates.Position, out uid, out grid);
    }
}
