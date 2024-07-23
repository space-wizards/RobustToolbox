using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.Physics;

public sealed partial class GridFixtureSystem
{
    /*
     * Something to keep in mind is that they rotate around the origin of a tile (e.g. 0,0), so a rotation of -90 degrees
     * moves the origin to 0, -1.
     */

    /// <summary>
    /// Merges GridB into GridA.
    /// </summary>
    /// <param name="offset">Origin of GridB relative to GridA</param>
    /// <param name="rotation">Rotation to apply to GridB when being merged.
    /// Note that the rotation is applied before the offset so the offset itself will be rotated.</param>
    public void Merge(
        EntityUid gridAUid,
        EntityUid gridBUid,
        Vector2i offset,
        Angle rotation,
        MapGridComponent? gridA = null,
        MapGridComponent? gridB = null,
        TransformComponent? xformA = null,
        TransformComponent? xformB = null)
    {
        var matrix = Matrix3Helpers.CreateTransform(offset, rotation);
        Merge(gridAUid, gridBUid, matrix, gridA, gridB, xformA, xformB);
    }

    /// <summary>
    /// Merges GridB into GridA.
    /// </summary>
    /// <param name="offset">Origin of GridB relative to GridA</param>
    /// <param name="matrix">Matrix to apply to gridB when being merged.
    /// Note that rotation is applied first and then offset so the offset itself will be rotated.</param>
    public void Merge(
        EntityUid gridAUid,
        EntityUid gridBUid,
        Matrix3x2 matrix,
        MapGridComponent? gridA = null,
        MapGridComponent? gridB = null,
        TransformComponent? xformA = null,
        TransformComponent? xformB = null)
    {
        if (!Resolve(gridAUid, ref gridA, ref xformA))
            return;

        if (!Resolve(gridBUid, ref gridB, ref xformB))
            return;

        var sw = new Stopwatch();
        var tiles = new List<(Vector2i Indices, Tile Tile)>();
        var enumerator = _maps.GetAllTilesEnumerator(gridBUid, gridB);

        while (enumerator.MoveNext(out var tileRef))
        {
            var offsetTile = Vector2.Transform(new Vector2(tileRef.Value.GridIndices.X, tileRef.Value.GridIndices.Y) + gridA.TileSizeHalfVector, matrix);
            tiles.Add((offsetTile.Floored(), tileRef.Value.Tile));
        }

        _maps.SetTiles(gridAUid, gridA, tiles);

        enumerator = _maps.GetAllTilesEnumerator(gridBUid, gridB);
        var rotationDiff = matrix.Rotation();

        while (enumerator.MoveNext(out var tileRef))
        {
            var chunkOrigin = SharedMapSystem.GetChunkIndices(tileRef.Value.GridIndices, gridB.ChunkSize);

            if (!_maps.TryGetChunk(gridBUid, gridB, chunkOrigin, out var chunk))
            {
                continue;
            }

            var chunkLocalTile = SharedMapSystem.GetChunkRelative(tileRef.Value.GridIndices, gridB.ChunkSize);
            var snapgrid = chunk.GetSnapGrid((ushort) chunkLocalTile.X, (ushort) chunkLocalTile.Y);

            if (snapgrid == null || snapgrid.Count == 0)
                continue;

            var offsetTile = Vector2.Transform(new Vector2(tileRef.Value.GridIndices.X, tileRef.Value.GridIndices.Y) + gridA.TileSizeHalfVector, matrix);
            var tileIndex = offsetTile.Floored();

            for (var j = snapgrid.Count - 1; j >= 0; j--)
            {
                var ent = snapgrid[j];
                var xform = _xformQuery.GetComponent(ent);
                _xformSystem.ReAnchor(ent, xform,
                    gridB, gridA,
                    tileRef.Value.GridIndices, tileIndex,
                    gridBUid, gridAUid,
                    xformB, xformA,
                    rotationDiff);

                DebugTools.Assert(xform.ParentUid == gridAUid);
            }

            DebugTools.Assert(snapgrid.Count == 0);
        }

        enumerator = _maps.GetAllTilesEnumerator(gridBUid, gridB);

        while (enumerator.MoveNext(out var tileRef))
        {
            var bounds = _lookup.GetLocalBounds(tileRef.Value.GridIndices, gridB.TileSize);

            _entSet.Clear();
            _lookup.GetLocalEntitiesIntersecting(gridBUid, bounds, _entSet, LookupFlags.All | ~LookupFlags.Contained | LookupFlags.Approximate);

            foreach (var ent in _entSet)
            {
                // Consider centre of entity position maybe?
                var entXform = _xformQuery.GetComponent(ent);

                if (entXform.ParentUid != gridBUid ||
                    !bounds.Contains(entXform.LocalPosition)) continue;

                var newPos = Vector2.Transform(entXform.LocalPosition, matrix);

                _xformSystem.SetCoordinates(ent, entXform, new EntityCoordinates(gridAUid, newPos), entXform.LocalRotation + rotationDiff, oldParent: xformB, newParent: xformA);
            }
        }

        DebugTools.Assert(xformB.ChildCount == 0);
        Del(gridBUid);

        Log.Debug($"Merged grids in {sw.Elapsed.TotalMilliseconds}ms");
    }
}
