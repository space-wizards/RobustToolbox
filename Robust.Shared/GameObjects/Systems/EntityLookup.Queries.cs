using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects;

public sealed partial class EntityLookupSystem
{
    #region Grid Methods

    /// <summary>
    /// Returns the entities intersecting any of the supplied tiles. Faster than doing each tile individually.
    /// </summary>
    /// <param name="gridId"></param>
    /// <param name="gridIndices"></param>
    /// <returns></returns>
    public IEnumerable<EntityUid> GetEntitiesIntersecting(GridId gridId, IEnumerable<Vector2i> gridIndices)
    {
        // Technically this doesn't consider anything overlapping from outside the grid but is this an issue?
        if (!_mapManager.TryGetGrid(gridId, out var grid)) return Enumerable.Empty<EntityUid>();

        var lookup = EntityManager.GetComponent<EntityLookupComponent>(grid.GridEntityId);
        var results = new HashSet<EntityUid>();
        var tileSize = grid.TileSize;

        // TODO: You can probably decompose the indices into larger areas if you take in a hashset instead.
        foreach (var index in gridIndices)
        {
            var aabb = GetLocalBounds(index, tileSize);

            lookup.Tree._b2Tree.FastQuery(ref aabb, (ref EntityUid data) =>
            {
                if (EntityManager.Deleted(data)) return;
                results.Add(data);
            });

            foreach (var ent in grid.GetAnchoredEntities(index))
            {
                if (EntityManager.Deleted(ent)) continue;
                results.Add(ent);
            }
        }

        return results;
    }

    public IEnumerable<EntityUid> GetEntitiesIntersecting(GridId gridId, Vector2i gridIndices)
    {
        // Technically this doesn't consider anything overlapping from outside the grid but is this an issue?
        if (!_mapManager.TryGetGrid(gridId, out var grid)) return Enumerable.Empty<EntityUid>();

        var lookup = EntityManager.GetComponent<EntityLookupComponent>(grid.GridEntityId);
        var tileSize = grid.TileSize;

        var aabb = GetLocalBounds(gridIndices, tileSize);
        var results = new HashSet<EntityUid>();

        lookup.Tree._b2Tree.FastQuery(ref aabb, (ref EntityUid data) =>
        {
            if (EntityManager.Deleted(data)) return;
            results.Add(data);
        });

        foreach (var ent in grid.GetAnchoredEntities(gridIndices))
        {
            if (EntityManager.Deleted(ent)) continue;
            results.Add(ent);
        }

        return results;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<EntityUid> GetEntitiesIntersecting(TileRef tileRef)
    {
        return GetEntitiesIntersecting(tileRef.GridIndex, tileRef.GridIndices);
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Box2 GetLocalBounds(Vector2i gridIndices, ushort tileSize)
    {
        return new Box2(gridIndices * tileSize, (gridIndices + 1) * tileSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Box2 GetLocalBounds(TileRef tileRef, ushort tileSize)
    {
        return GetLocalBounds(tileRef.GridIndices, tileSize);
    }

    public Box2Rotated GetWorldBounds(TileRef tileRef, Matrix3? worldMatrix = null, Angle? angle = null)
    {
        var grid = _mapManager.GetGrid(tileRef.GridIndex);

        if (worldMatrix == null || angle == null)
        {
            var gridXform = EntityManager.GetComponent<TransformComponent>(grid.GridEntityId);
            var (_, wAng, wMat) = gridXform.GetWorldPositionRotationMatrix();
            worldMatrix = wMat;
            angle = wAng;
        }

        var center = worldMatrix.Value.Transform((Vector2) tileRef.GridIndices + 0.5f) * grid.TileSize;
        var translatedBox = Box2.CenteredAround(center, (grid.TileSize, grid.TileSize));

        return new Box2Rotated(translatedBox, -angle.Value, center);
    }
}
