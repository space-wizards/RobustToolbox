using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Server.Physics;

internal sealed partial class GridFixtureSystem
{
    /// <summary>
    /// Merges GridB into GridA.
    /// </summary>
    /// <param name="offset">Origin of GridB relative to GridA</param>
    public void Merge(
        EntityUid gridUidA,
        EntityUid gridUidB,
        Vector2i offset,
        Angle rotation,
        MapGridComponent? gridA = null,
        MapGridComponent? gridB = null,
        TransformComponent? xformA = null,
        TransformComponent? xformB = null)
    {
        if (!Resolve(gridUidA, ref gridA, ref xformA))
            return;

        if (!Resolve(gridUidB, ref gridB, ref xformB))
            return;

        var tiles = new List<(Vector2i Indices, Tile Tile)>();
        var enumerator = gridB.GetAllTilesEnumerator();
        var xformQuery = GetEntityQuery<TransformComponent>();

        while (enumerator.MoveNext(out var tileRef))
        {
            var offsetTile = tileRef.Value.GridIndices.Rotate(rotation);
            offsetTile += offset;
            tiles.Add((offsetTile, tileRef.Value.Tile));
        }

        gridA.SetTiles(tiles);

        enumerator = gridB.GetAllTilesEnumerator();

        while (enumerator.MoveNext(out var tileRef))
        {
            if (!gridB.TryGetChunk(tileRef.Value.GridIndices, out var chunk))
            {
                continue;
            }

            var chunkLocalTile = tileRef.Value.GridIndices - chunk.Indices;
            var snapgrid = chunk.GetSnapGrid((ushort) chunkLocalTile.X, (ushort) chunkLocalTile.Y);

            if (snapgrid == null || snapgrid.Count == 0) continue;

            var offsetTile = tileRef.Value.GridIndices.Rotate(rotation) + offset;

            for (var j = snapgrid.Count - 1; j >= 0; j--)
            {
                var ent = snapgrid[j];
                var xform = xformQuery.GetComponent(ent);
                _xformSystem.ReAnchor(xform, gridB, gridA, offsetTile, xformB, xformA, xformQuery);
                DebugTools.Assert(xform.Anchored);
            }
        }

        enumerator = gridB.GetAllTilesEnumerator();

        while (enumerator.MoveNext(out var tileRef))
        {
            var bounds = _lookup.GetLocalBounds(tileRef.Value.GridIndices, gridB.TileSize);

            foreach (var ent in _lookup.GetEntitiesIntersecting(gridB.Owner, tileRef.Value.GridIndices))
            {
                // Consider centre of entity position maybe?
                var entXform = xformQuery.GetComponent(ent);

                if (entXform.ParentUid != gridB.Owner ||
                    !bounds.Contains(entXform.LocalPosition)) continue;

                _xformSystem.SetParent(entXform, gridA.Owner, xformQuery, xformA);
            }
        }

        DebugTools.Assert(xformB.ChildCount == 0);
        Del(gridB.Owner);
    }
}
