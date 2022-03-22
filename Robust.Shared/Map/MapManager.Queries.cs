using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Robust.Shared.Map;

internal partial class MapManager
{
    public IEnumerable<IMapGrid> FindGridsIntersecting(MapId mapId, Box2Rotated bounds, bool approx = false)
    {
        var aabb = bounds.CalcBoundingBox();
        // TODO: We can do slower GJK checks to check if 2 bounds actually intersect, but WYCI.
        return FindGridsIntersecting(mapId, aabb, approx);
    }

    public IEnumerable<IMapGrid> FindGridsIntersecting(MapId mapId, Box2 worldAabb, bool approx = false)
    {
        if (!_gridTrees.ContainsKey(mapId)) return Enumerable.Empty<IMapGrid>();

        var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();
        var physicsQuery = EntityManager.GetEntityQuery<PhysicsComponent>();
        var grids = new List<MapGrid>();

        return FindGridsIntersecting(mapId, worldAabb, grids, xformQuery, physicsQuery, approx);
    }

    /// <summary>
    /// Returns the grids intersecting this AABB.
    /// </summary>
    public IEnumerable<IMapGrid> FindGridsIntersecting(
        MapId mapId,
        Box2 aabb,
        List<MapGrid> grids,
        EntityQuery<TransformComponent> xformQuery,
        EntityQuery<PhysicsComponent> physicsQuery,
        bool approx = false)
    {
        if (!_gridTrees.TryGetValue(mapId, out var gridTree)) return Enumerable.Empty<IMapGrid>();

        DebugTools.Assert(grids.Count == 0);
        var state = (gridTree, grids);

        gridTree.Query(ref state,
            static (ref (B2DynamicTree<MapGrid> gridTree, List<MapGrid> grids) tuple, DynamicTree.Proxy proxy) =>
            {
                // Paul's gonna seethe over nullable suppression but if the user data is null here you're gonna have bigger problems.
                tuple.grids.Add(tuple.gridTree.GetUserData(proxy)!);
                return true;
            }, in aabb);


        if (!approx)
        {
            for (var i = grids.Count - 1; i >= 0; i--)
            {
                var grid = grids[i];

                var xformComp = xformQuery.GetComponent(grid.GridEntityId);
                var (worldPos, worldRot, invMatrix) = xformComp.GetWorldPositionRotationInvMatrix(xformQuery);
                var localAABB = invMatrix.TransformBox(aabb);

                var intersects = false;

                if (physicsQuery.HasComponent(grid.GridEntityId))
                {
                    grid.GetLocalMapChunks(localAABB, out var enumerator);

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

    /// <summary>
    /// Attempts to find the map grid under the map location.
    /// </summary>
    public bool TryFindGridAt(MapId mapId, Vector2 worldPos, [NotNullWhen(true)] out IMapGrid? grid)
    {
        // Need to enlarge the AABB by at least the grid shrinkage size.
        var aabb = new Box2(worldPos - 0.5f, worldPos + 0.5f);
        var grids = FindGridsIntersecting(mapId, aabb, true);

        foreach (var gridInter in grids)
        {
            var mapGrid = (MapGrid) gridInter;

            // Turn the worldPos into a localPos and work out the relevant chunk we need to check
            // This is much faster than iterating over every chunk individually.
            // (though now we need some extra calcs up front).

            // Doesn't use WorldBounds because it's just an AABB.
            var matrix = EntityManager.GetComponent<TransformComponent>(mapGrid.GridEntityId).InvWorldMatrix;
            var localPos = matrix.Transform(worldPos);

            // NOTE:
            // If you change this to use fixtures instead (i.e. if you want half-tiles) then you need to make sure
            // you account for the fact that fixtures are shrunk slightly!
            var tile = new Vector2i((int) Math.Floor(localPos.X), (int) Math.Floor(localPos.Y));
            var chunkIndices = mapGrid.GridTileToChunkIndices(tile);

            if (!mapGrid.HasChunk(chunkIndices)) continue;

            var chunk = mapGrid.GetChunk(chunkIndices);
            Vector2i indices = chunk.GridTileToChunkTile(tile);
            var chunkTile = chunk.GetTile((ushort)indices.X, (ushort)indices.Y);

            if (chunkTile.IsEmpty) continue;
            grid = mapGrid;
            return true;
        }

        grid = null;
        return false;
    }

    /// <summary>
    /// Attempts to find the map grid under the map location.
    /// </summary>
    public bool TryFindGridAt(MapCoordinates mapCoordinates, [NotNullWhen(true)] out IMapGrid? grid)
    {
        return TryFindGridAt(mapCoordinates.MapId, mapCoordinates.Position, out grid);
    }
}
