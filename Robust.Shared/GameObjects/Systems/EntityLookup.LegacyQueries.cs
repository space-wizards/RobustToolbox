using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public sealed partial class EntityLookupSystem
{
    /// <inheritdoc />
    public IEnumerable<EntityUid> GetEntitiesInRange(EntityCoordinates position, float range, LookupFlags flags = LookupFlags.IncludeAnchored)
    {
        var mapCoordinates = position.ToMap(EntityManager);
        var mapPosition = mapCoordinates.Position;
        var aabb = new Box2(mapPosition - new Vector2(range, range),
            mapPosition + new Vector2(range, range));
        return GetEntitiesIntersecting(mapCoordinates.MapId, aabb, flags);
        // TODO: Use a circle shape here mate
    }

    /// <inheritdoc />
    public IEnumerable<EntityUid> GetEntitiesInRange(MapId mapId, Box2 box, float range, LookupFlags flags = LookupFlags.IncludeAnchored)
    {
        var aabb = box.Enlarged(range);
        return GetEntitiesIntersecting(mapId, aabb, flags);
    }

    /// <inheritdoc />
    public IEnumerable<EntityUid> GetEntitiesInRange(MapId mapId, Vector2 point, float range, LookupFlags flags = LookupFlags.IncludeAnchored)
    {
        var aabb = new Box2(point, point).Enlarged(range);
        return GetEntitiesIntersecting(mapId, aabb, flags);
    }

    /// <inheritdoc />
    public IEnumerable<EntityUid> GetEntitiesInRange(EntityUid entity, float range, LookupFlags flags = LookupFlags.IncludeAnchored)
    {
        var worldAABB = GetWorldAABB(entity);
        return GetEntitiesInRange(EntityManager.GetComponent<TransformComponent>(entity).MapID, worldAABB, range, flags);
    }

    /// <inheritdoc />
    public IEnumerable<EntityUid> GetEntitiesInArc(EntityCoordinates coordinates, float range, Angle direction,
        float arcWidth, LookupFlags flags = LookupFlags.IncludeAnchored)
    {
        var position = coordinates.ToMap(EntityManager).Position;

        foreach (var entity in GetEntitiesInRange(coordinates, range * 2, flags))
        {
            var angle = new Angle(EntityManager.GetComponent<TransformComponent>(entity).WorldPosition - position);
            if (angle.Degrees < direction.Degrees + arcWidth / 2 &&
                angle.Degrees > direction.Degrees - arcWidth / 2)
                yield return entity;
        }
    }

    /// <inheritdoc />
    public IEnumerable<EntityUid> GetEntitiesInMap(MapId mapId, LookupFlags flags = LookupFlags.IncludeAnchored)
    {
        DebugTools.Assert((flags & LookupFlags.Approximate) == 0x0);

        foreach (EntityLookupComponent comp in EntityManager.EntityQuery<EntityLookupComponent>(true))
        {
            if (EntityManager.GetComponent<TransformComponent>(comp.Owner).MapID != mapId) continue;

            foreach (var entity in comp.Tree)
            {
                if (EntityManager.Deleted(entity)) continue;

                yield return entity;
            }
        }

        if ((flags & LookupFlags.IncludeAnchored) == 0x0) yield break;

        foreach (var grid in _mapManager.GetAllMapGrids(mapId))
        {
            foreach (var tile in grid.GetAllTiles())
            {
                foreach (var uid in grid.GetAnchoredEntities(tile.GridIndices))
                {
                    if (!EntityManager.EntityExists(uid)) continue;
                    yield return uid;
                }
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<EntityUid> GetEntitiesAt(MapId mapId, Vector2 position, LookupFlags flags = LookupFlags.IncludeAnchored)
    {
        if (mapId == MapId.Nullspace) return Enumerable.Empty<EntityUid>();

        var list = new List<EntityUid>();

        var state = (list, position);

        var aabb = new Box2(position, position).Enlarged(PointEnlargeRange);
        var enumerator = GetLookupsIntersecting(mapId, aabb);

        while (enumerator.MoveNext(out var lookup))
        {
            var offsetPos = EntityManager.GetComponent<TransformComponent>(lookup.Owner).InvWorldMatrix.Transform(position);

            lookup.Tree.QueryPoint(ref state, (ref (List<EntityUid> list, Vector2 position) state, in EntityUid ent) =>
            {
                state.list.Add(ent);
                return true;
            }, offsetPos, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.IncludeAnchored) != 0x0)
        {
            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, aabb))
            {
                foreach (var uid in grid.GetAnchoredEntities(aabb))
                {
                    if (!EntityManager.EntityExists(uid)) continue;
                    list.Add(uid);
                }
            }
        }

        return list;
    }
}
