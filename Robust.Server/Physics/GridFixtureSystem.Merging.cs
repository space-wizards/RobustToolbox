using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Server.Physics;

public sealed partial class GridFixtureSystem
{
    /// <summary>
    /// Merges GridB into GridA.
    /// </summary>
    /// <param name="offset">Origin of GridB relative to GridA</param>
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
        var matrix = Matrix3.CreateTransform(offset, rotation);
        Merge(gridAUid, gridBUid, matrix, gridA, gridB, xformA, xformB);
    }

    /// <summary>
    /// Merges GridB into GridA.
    /// </summary>
    /// <param name="offset">Origin of GridB relative to GridA</param>
    public void Merge(
        EntityUid gridAUid,
        EntityUid gridBUid,
        Matrix3 matrix,
        MapGridComponent? gridA = null,
        MapGridComponent? gridB = null,
        TransformComponent? xformA = null,
        TransformComponent? xformB = null)
    {
        if (!Resolve(gridAUid, ref gridA, ref xformA))
            return;

        if (!Resolve(gridBUid, ref gridB, ref xformB))
            return;

        var tiles = new List<(Vector2i Indices, Tile Tile)>();
        var enumerator = _maps.GetAllTilesEnumerator(gridBUid, gridB);

        while (enumerator.MoveNext(out var tileRef))
        {
            var offsetTile = matrix.Transform(new Vector2(tileRef.Value.GridIndices.X, tileRef.Value.GridIndices.Y) + gridA.TileSizeHalfVector);
            tiles.Add((offsetTile.Floored(), tileRef.Value.Tile));
        }

        _maps.SetTiles(gridAUid, gridA, tiles);

        enumerator = _maps.GetAllTilesEnumerator(gridBUid, gridB);

        while (enumerator.MoveNext(out var tileRef))
        {
            var chunkOrigin = SharedMapSystem.GetChunkIndices(tileRef.Value.GridIndices, gridB.ChunkSize);

            if (!_maps.TryGetChunk(gridBUid, gridB, chunkOrigin, out var chunk))
            {
                continue;
            }

            var chunkLocalTile = tileRef.Value.GridIndices - chunk.Indices;
            var snapgrid = chunk.GetSnapGrid((ushort) chunkLocalTile.X, (ushort) chunkLocalTile.Y);

            if (snapgrid == null || snapgrid.Count == 0) continue;

            var offsetTile = matrix.Transform(new Vector2(tileRef.Value.GridIndices.X, tileRef.Value.GridIndices.Y) + gridA.TileSizeHalfVector);

            for (var j = snapgrid.Count - 1; j >= 0; j--)
            {
                var ent = snapgrid[j];
                var xform = _xformQuery.GetComponent(ent);
                _xformSystem.ReAnchor(ent, xform, gridB, gridA, offsetTile.Floored(), gridBUid, gridAUid, xformB, xformA);
                DebugTools.Assert(xform.Anchored);
                DebugTools.Assert(xform.ParentUid == gridAUid);
            }
        }

        enumerator = _maps.GetAllTilesEnumerator(gridBUid, gridB);

        while (enumerator.MoveNext(out var tileRef))
        {
            var bounds = _lookup.GetLocalBounds(tileRef.Value.GridIndices, gridB.TileSize);

            _entSet.Clear();
            _lookup.GetLocalEntitiesIntersecting(gridBUid, bounds, _entSet);

            foreach (var ent in _entSet)
            {
                // Consider centre of entity position maybe?
                var entXform = _xformQuery.GetComponent(ent);

                if (entXform.ParentUid != gridBUid ||
                    !bounds.Contains(entXform.LocalPosition)) continue;

                _xformSystem.SetParent(ent, entXform, gridAUid);
            }
        }

        DebugTools.Assert(xformB.ChildCount == 0);
        Del(gridBUid);
    }
}
