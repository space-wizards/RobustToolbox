using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;

namespace Robust.Shared.Map;

internal partial class MapManager
{
    public IEnumerable<MapGridComponent> FindGridsIntersecting(MapId mapId, Box2Rotated bounds, bool approx = false)
    {
        var aabb = bounds.CalcBoundingBox();
        // TODO: We can do slower GJK checks to check if 2 bounds actually intersect, but WYCI.
        return FindGridsIntersecting(mapId, aabb, approx);
    }

    public void FindGridsIntersectingApprox(MapId mapId, Box2 worldAABB, GridCallback callback)
    {
        if (!_gridTrees.TryGetValue(mapId, out var gridTree))
            return;

        if (EntityManager.TryGetComponent<MapGridComponent>(GetMapEntityId(mapId), out var grid))
        {
            callback(grid);
        }

        var state = (gridTree, callback);

        gridTree.Query(ref state, static (ref (
                B2DynamicTree<MapGridComponent> gridTree,
                GridCallback callback) tuple,
                DynamicTree.Proxy proxy) =>
        {
            var data = tuple.gridTree.GetUserData(proxy);
            tuple.callback(data!);
            return true;
        }, worldAABB);
    }

    public void FindGridsIntersectingApprox<TState>(MapId mapId, Box2 worldAABB, ref TState state, GridCallback<TState> callback)
    {
        if (!_gridTrees.TryGetValue(mapId, out var gridTree))
            return;

        var state2 = (state, gridTree, callback);

        gridTree.Query(ref state2, static (ref (
                TState state,
                B2DynamicTree<MapGridComponent> gridTree,
                GridCallback<TState> callback) tuple,
            DynamicTree.Proxy proxy) =>
        {
            var data = tuple.gridTree.GetUserData(proxy);
            return tuple.callback(data!, ref tuple.state);
        }, worldAABB);

        if (EntityManager.TryGetComponent<MapGridComponent>(GetMapEntityId(mapId), out var grid))
        {
            callback(grid, ref state);
        }

        state = state2.state;
    }

    public IEnumerable<MapGridComponent> FindGridsIntersecting(MapId mapId, Box2 worldAabb, bool approx = false)
    {
        if (!_gridTrees.ContainsKey(mapId)) return Enumerable.Empty<MapGridComponent>();

        var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();
        var physicsQuery = EntityManager.GetEntityQuery<PhysicsComponent>();
        var grids = new List<MapGridComponent>();

        return FindGridsIntersecting(mapId, worldAabb, grids, xformQuery, physicsQuery, approx);
    }

    /// <inheritdoc />
    public IEnumerable<MapGridComponent> FindGridsIntersecting(
        MapId mapId,
        Box2 aabb,
        List<MapGridComponent> grids,
        EntityQuery<TransformComponent> xformQuery,
        EntityQuery<PhysicsComponent> physicsQuery,
        bool approx = false)
    {
        if (!_gridTrees.TryGetValue(mapId, out var gridTree)) return Enumerable.Empty<MapGridComponent>();

        DebugTools.Assert(grids.Count == 0);
        var offset = 0;

        if (EntityManager.TryGetComponent<MapGridComponent>(GetMapEntityId(mapId), out var mapGrid))
        {
            grids.Add(mapGrid);
            offset = 1;
        }

        var state = (gridTree, grids);

        gridTree.Query(ref state,
            static (ref (B2DynamicTree<MapGridComponent> gridTree, List<MapGridComponent> grids) tuple, DynamicTree.Proxy proxy) =>
            {
                // Paul's gonna seethe over nullable suppression but if the user data is null here you're gonna have bigger problems.
                tuple.grids.Add(tuple.gridTree.GetUserData(proxy)!);
                return true;
            }, in aabb);

        if (!approx)
        {
            for (var i = grids.Count - 1; i >= offset; i--)
            {
                var grid = grids[i];

                var xformComp = xformQuery.GetComponent(grid.Owner);
                var (worldPos, worldRot, matrix, invMatrix) = xformComp.GetWorldPositionRotationMatrixWithInv(xformQuery);
                var overlap = matrix.TransformBox(grid.LocalAABB).Intersect(aabb);
                var localAABB = invMatrix.TransformBox(overlap);

                var intersects = false;

                if (physicsQuery.HasComponent(grid.Owner))
                {
                    var enumerator = grid.GetLocalMapChunks(localAABB);

                    var transform = new Transform(worldPos, worldRot);

                    while (!intersects && enumerator.MoveNext(out var chunk))
                    {
                        foreach (var fixture in chunk.Fixtures)
                        {
                            for (var j = 0; j < fixture.Shape.ChildCount; j++)
                            {
                                if (!fixture.Shape.ComputeAABB(transform, j).Intersects(aabb)) continue;

                                intersects = true;
                                break;
                            }

                            if (intersects) break;
                        }
                    }
                }

                if (intersects || grid.ChunkCount == 0 && aabb.Contains(worldPos)) continue;

                grids.RemoveSwap(i);
            }
        }

        return grids;
    }

    public bool TryFindGridAt(
        MapId mapId,
        Vector2 worldPos,
        EntityQuery<TransformComponent> xformQuery,
        [NotNullWhen(true)] out MapGridComponent? grid)
    {
        // Need to enlarge the AABB by at least the grid shrinkage size.
        var aabb = new Box2(worldPos - 0.2f, worldPos + 0.2f);

        grid = null;
        var state = (grid, worldPos, xformQuery);

        FindGridsIntersectingApprox(mapId, aabb, ref state, static (MapGridComponent iGrid, ref (MapGridComponent? grid, Vector2 worldPos, EntityQuery<TransformComponent> xformQuery) tuple) =>
        {
            // Turn the worldPos into a localPos and work out the relevant chunk we need to check
            // This is much faster than iterating over every chunk individually.
            // (though now we need some extra calcs up front).

            // Doesn't use WorldBounds because it's just an AABB.
            var matrix = tuple.xformQuery.GetComponent(iGrid.Owner).InvWorldMatrix;
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

            tuple.grid = iGrid;
            return false;
        });

        if (state.grid == null && EntityManager.TryGetComponent<MapGridComponent>(GetMapEntityId(mapId), out var mapGrid))
        {
            grid = mapGrid;
            return true;
        }

        grid = state.grid;
        return grid != null;
    }

    /// <summary>
    /// Attempts to find the map grid under the map location.
    /// </summary>
    public bool TryFindGridAt(MapId mapId, Vector2 worldPos, [NotNullWhen(true)] out MapGridComponent? grid)
    {
        var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();
        return TryFindGridAt(mapId, worldPos, xformQuery, out grid);
    }

    /// <summary>
    /// Attempts to find the map grid under the map location.
    /// </summary>
    public bool TryFindGridAt(MapCoordinates mapCoordinates, [NotNullWhen(true)] out MapGridComponent? grid)
    {
        return TryFindGridAt(mapCoordinates.MapId, mapCoordinates.Position, out grid);
    }
}
