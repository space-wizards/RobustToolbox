using System.Collections.Generic;
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
                var xform = _xformQuery.GetComponent(ent);
                _xformSystem.ReAnchor(ent, xform, gridB, gridA, offsetTile, gridUidB, gridUidA, xformB, xformA);
                DebugTools.Assert(xform.Anchored);
            }
        }

        enumerator = gridB.GetAllTilesEnumerator();

        while (enumerator.MoveNext(out var tileRef))
        {
            var bounds = _lookup.GetLocalBounds(tileRef.Value.GridIndices, gridB.TileSize);

            _entSet.Clear();
            _lookup.GetEntitiesIntersecting(gridUidB, tileRef.Value.GridIndices, _entSet);

            foreach (var ent in _entSet)
            {
                // Consider centre of entity position maybe?
                var entXform = _xformQuery.GetComponent(ent);

                if (entXform.ParentUid != gridUidB ||
                    !bounds.Contains(entXform.LocalPosition)) continue;

                _xformSystem.SetParent(ent, entXform, gridUidA);
            }
        }

        DebugTools.Assert(xformB.ChildCount == 0);
        Del(gridUidB);
    }
}
