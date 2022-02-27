using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public sealed partial class EntityLookup
{
    // TODO: Need to nuke most of the below and cleanup when entitylookup gets optimised some more (physics + containers).

    private LookupsEnumerator GetLookupsIntersecting(MapId mapId, Box2 worldAABB)
    {
        _mapManager.FindGridsIntersectingEnumerator(mapId, worldAABB, out var gridEnumerator, true);

        return new LookupsEnumerator(_entityManager, _mapManager, mapId, gridEnumerator);
    }

    private struct LookupsEnumerator
    {
        private IEntityManager _entityManager;
        private IMapManager _mapManager;

        private MapId _mapId;
        private FindGridsEnumerator _enumerator;

        private bool _final;

        public LookupsEnumerator(IEntityManager entityManager, IMapManager mapManager, MapId mapId, FindGridsEnumerator enumerator)
        {
            _entityManager = entityManager;
            _mapManager = mapManager;

            _mapId = mapId;
            _enumerator = enumerator;
            _final = false;
        }

        public bool MoveNext([NotNullWhen(true)] out EntityLookupComponent? component)
        {
            if (!_enumerator.MoveNext(out var grid))
            {
                if (_final || _mapId == MapId.Nullspace)
                {
                    component = null;
                    return false;
                }

                _final = true;
                EntityUid mapUid = _mapManager.GetMapEntityIdOrThrow(_mapId);
                component = _entityManager.GetComponent<EntityLookupComponent>(mapUid);
                return true;
            }

            // TODO: Recursive and all that.
            component = _entityManager.GetComponent<EntityLookupComponent>(grid.GridEntityId);
            return true;
        }
    }

    private IEnumerable<EntityUid> GetAnchored(MapId mapId, Box2 worldAABB, LookupFlags flags)
    {
        if ((flags & LookupFlags.IncludeAnchored) == 0x0) yield break;
        foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
        {
            foreach (var uid in grid.GetAnchoredEntities(worldAABB))
            {
                if (!_entityManager.EntityExists(uid)) continue;
                yield return uid;
            }
        }
    }

    private IEnumerable<EntityUid> GetAnchored(MapId mapId, Box2Rotated worldBounds, LookupFlags flags)
    {
        if ((flags & LookupFlags.IncludeAnchored) == 0x0) yield break;
        foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
        {
            foreach (var uid in grid.GetAnchoredEntities(worldBounds))
            {
                if (!_entityManager.EntityExists(uid)) continue;
                yield return uid;
            }
        }
    }

    /// <inheritdoc />
    public bool AnyEntitiesIntersecting(MapId mapId, Box2 box, LookupFlags flags = LookupFlags.IncludeAnchored)
    {
        var found = false;
        var enumerator = GetLookupsIntersecting(mapId, box);

        while (enumerator.MoveNext(out var lookup))
        {
            var offsetBox = _entityManager.GetComponent<TransformComponent>(lookup.Owner).InvWorldMatrix.TransformBox(box);

            lookup.Tree.QueryAabb(ref found, (ref bool found, in EntityUid ent) =>
            {
                if (_entityManager.Deleted(ent))
                    return true;

                found = true;
                return false;

            }, offsetBox, (flags & LookupFlags.Approximate) != 0x0);
        }

        if (!found)
        {
            foreach (var _ in GetAnchored(mapId, box, flags))
            {
                return true;
            }
        }

        return found;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void FastEntitiesIntersecting(in MapId mapId, ref Box2 worldAABB, EntityUidQueryCallback callback, LookupFlags flags = LookupFlags.IncludeAnchored)
    {
        var enumerator = GetLookupsIntersecting(mapId, worldAABB);
        while (enumerator.MoveNext(out var lookup))
        {
            var offsetBox = _entityManager.GetComponent<TransformComponent>(lookup.Owner).InvWorldMatrix.TransformBox(worldAABB);

            lookup.Tree._b2Tree.FastQuery(ref offsetBox, (ref EntityUid data) => callback(data));
        }

        if ((flags & LookupFlags.IncludeAnchored) != 0x0)
        {
            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
            {
                foreach (var uid in grid.GetAnchoredEntities(worldAABB))
                {
                    if (!_entityManager.EntityExists(uid)) continue;
                    callback(uid);
                }
            }
        }
    }

    /// <inheritdoc />
    public void FastEntitiesIntersecting(EntityLookupComponent lookup, ref Box2 localAABB, EntityUidQueryCallback callback)
    {
        lookup.Tree._b2Tree.FastQuery(ref localAABB, (ref EntityUid data) => callback(data));
    }

    /// <inheritdoc />
    public IEnumerable<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2 worldAABB, LookupFlags flags = LookupFlags.IncludeAnchored)
    {
        if (mapId == MapId.Nullspace) return Enumerable.Empty<EntityUid>();

        var list = new List<EntityUid>();
        var enumerator = GetLookupsIntersecting(mapId, worldAABB);

        while (enumerator.MoveNext(out var lookup))
        {
            var offsetBox = _entityManager.GetComponent<TransformComponent>(lookup.Owner).InvWorldMatrix.TransformBox(worldAABB);

            lookup.Tree.QueryAabb(ref list, (ref List<EntityUid> list, in EntityUid ent) =>
            {
                if (!_entityManager.Deleted(ent))
                {
                    list.Add(ent);
                }
                return true;
            }, offsetBox, (flags & LookupFlags.Approximate) != 0x0);
        }

        foreach (var ent in GetAnchored(mapId, worldAABB, flags))
        {
            list.Add(ent);
        }

        return list;
    }

    /// <inheritdoc />
    public IEnumerable<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2Rotated worldBounds, LookupFlags flags = LookupFlags.IncludeAnchored)
    {
        if (mapId == MapId.Nullspace) return Enumerable.Empty<EntityUid>();

        var list = new List<EntityUid>();
        var worldAABB = worldBounds.CalcBoundingBox();
        var enumerator = GetLookupsIntersecting(mapId, worldAABB);

        while (enumerator.MoveNext(out var lookup))
        {
            var offsetBox = _entityManager.GetComponent<TransformComponent>(lookup.Owner).InvWorldMatrix.TransformBox(worldBounds);

            lookup.Tree.QueryAabb(ref list, (ref List<EntityUid> list, in EntityUid ent) =>
            {
                if (!_entityManager.Deleted(ent))
                {
                    list.Add(ent);
                }
                return true;
            }, offsetBox, (flags & LookupFlags.Approximate) != 0x0);
        }

        foreach (var ent in GetAnchored(mapId, worldBounds, flags))
        {
            list.Add(ent);
        }

        return list;
    }

    /// <inheritdoc />
    public IEnumerable<EntityUid> GetEntitiesIntersecting(MapId mapId, Vector2 position, LookupFlags flags = LookupFlags.IncludeAnchored)
    {
        if (mapId == MapId.Nullspace) return Enumerable.Empty<EntityUid>();

        var aabb = new Box2(position, position).Enlarged(PointEnlargeRange);
        var list = new List<EntityUid>();
        var state = (list, position);

        var enumerator = GetLookupsIntersecting(mapId, aabb);

        while (enumerator.MoveNext(out var lookup))
        {
            var localPoint = _entityManager.GetComponent<TransformComponent>(lookup.Owner).InvWorldMatrix.Transform(position);

            lookup.Tree.QueryPoint(ref state, (ref (List<EntityUid> list, Vector2 position) state, in EntityUid ent) =>
            {
                if (Intersecting(ent, state.position))
                {
                    state.list.Add(ent);
                }
                return true;
            }, localPoint, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.IncludeAnchored) != 0x0 &&
            _mapManager.TryFindGridAt(mapId, position, out var grid) &&
            grid.TryGetTileRef(position, out var tile))
        {
            foreach (var uid in grid.GetAnchoredEntities(tile.GridIndices))
            {
                if (!_entityManager.EntityExists(uid)) continue;
                state.list.Add(uid);
            }
        }

        return list;
    }

    /// <inheritdoc />
    public IEnumerable<EntityUid> GetEntitiesIntersecting(MapCoordinates position, LookupFlags flags = LookupFlags.IncludeAnchored)
    {
        return GetEntitiesIntersecting(position.MapId, position.Position, flags);
    }

    /// <inheritdoc />
    public IEnumerable<EntityUid> GetEntitiesIntersecting(EntityCoordinates position, LookupFlags flags = LookupFlags.IncludeAnchored)
    {
        var mapPos = position.ToMap(_entityManager);
        return GetEntitiesIntersecting(mapPos.MapId, mapPos.Position, flags);
    }

    /// <inheritdoc />
    public IEnumerable<EntityUid> GetEntitiesIntersecting(EntityUid entity, float enlarged = 0f, LookupFlags flags = LookupFlags.IncludeAnchored)
    {
        var worldAABB = GetWorldAABB(entity);
        var xform = _entityManager.GetComponent<TransformComponent>(entity);

        var (worldPos, worldRot) = xform.GetWorldPositionRotation();

        var enumerator = GetLookupsIntersecting(xform.MapID, worldAABB);
        var list = new List<EntityUid>();

        while (enumerator.MoveNext(out var lookup))
        {
            // To get the tightest bounds possible we'll re-calculate it for each lookup.
            var localBounds = GetLookupBounds(entity, lookup, worldPos, worldRot, enlarged);

            lookup.Tree.QueryAabb(ref list, (ref List<EntityUid> list, in EntityUid ent) =>
            {
                if (!_entityManager.Deleted(ent))
                {
                    list.Add(ent);
                }
                return true;
            }, localBounds, (flags & LookupFlags.Approximate) != 0x0);
        }

        foreach (var ent in GetAnchored(xform.MapID, worldAABB, flags))
        {
            list.Add(ent);
        }

        return list;
    }

    private Box2 GetLookupBounds(EntityUid uid, EntityLookupComponent lookup, Vector2 worldPos, Angle worldRot, float enlarged)
    {
        var (_, lookupRot, lookupInvWorldMatrix) = _entityManager.GetComponent<TransformComponent>(lookup.Owner).GetWorldPositionRotationInvMatrix();

        var localPos = lookupInvWorldMatrix.Transform(worldPos);
        var localRot = worldRot - lookupRot;

        if (_entityManager.TryGetComponent(uid, out FixturesComponent? manager))
        {
            var transform = new Transform(localPos, localRot);
            Box2? aabb = null;

            foreach (var (_, fixture) in manager.Fixtures)
            {
                if (!fixture.Hard) continue;
                for (var i = 0; i < fixture.Shape.ChildCount; i++)
                {
                    aabb = aabb?.Union(fixture.Shape.ComputeAABB(transform, i)) ?? fixture.Shape.ComputeAABB(transform, i);
                }
            }

            if (aabb != null)
            {
                return aabb.Value.Enlarged(enlarged);
            }
        }

        // So IsEmpty checks don't get triggered
        return new Box2(localPos - float.Epsilon, localPos + float.Epsilon);
    }

    /// <inheritdoc />
    public bool IsIntersecting(EntityUid entityOne, EntityUid entityTwo)
    {
        var position = _entityManager.GetComponent<TransformComponent>(entityOne).MapPosition.Position;
        return Intersecting(entityTwo, position);
    }

    private bool Intersecting(EntityUid entity, Vector2 mapPosition)
    {
        if (_entityManager.TryGetComponent(entity, out IPhysBody? component))
        {
            if (component.GetWorldAABB().Contains(mapPosition))
                return true;
        }
        else
        {
            var transform = _entityManager.GetComponent<TransformComponent>(entity);
            var entPos = transform.WorldPosition;
            if (MathHelper.CloseToPercent(entPos.X, mapPosition.X)
                && MathHelper.CloseToPercent(entPos.Y, mapPosition.Y))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public IEnumerable<EntityUid> GetEntitiesInRange(EntityCoordinates position, float range, LookupFlags flags = LookupFlags.IncludeAnchored)
    {
        var mapCoordinates = position.ToMap(_entityManager);
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
        return GetEntitiesInRange(_entityManager.GetComponent<TransformComponent>(entity).MapID, worldAABB, range, flags);
    }

    /// <inheritdoc />
    public IEnumerable<EntityUid> GetEntitiesInArc(EntityCoordinates coordinates, float range, Angle direction,
        float arcWidth, LookupFlags flags = LookupFlags.IncludeAnchored)
    {
        var position = coordinates.ToMap(_entityManager).Position;

        foreach (var entity in GetEntitiesInRange(coordinates, range * 2, flags))
        {
            var angle = new Angle(_entityManager.GetComponent<TransformComponent>(entity).WorldPosition - position);
            if (angle.Degrees < direction.Degrees + arcWidth / 2 &&
                angle.Degrees > direction.Degrees - arcWidth / 2)
                yield return entity;
        }
    }

    /// <inheritdoc />
    public IEnumerable<EntityUid> GetEntitiesInMap(MapId mapId, LookupFlags flags = LookupFlags.IncludeAnchored)
    {
        DebugTools.Assert((flags & LookupFlags.Approximate) == 0x0);

        foreach (EntityLookupComponent comp in _entityManager.EntityQuery<EntityLookupComponent>(true))
        {
            if (_entityManager.GetComponent<TransformComponent>(comp.Owner).MapID != mapId) continue;

            foreach (var entity in comp.Tree)
            {
                if (_entityManager.Deleted(entity)) continue;

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
                    if (!_entityManager.EntityExists(uid)) continue;
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
            var offsetPos = _entityManager.GetComponent<TransformComponent>(lookup.Owner).InvWorldMatrix.Transform(position);

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
                    if (!_entityManager.EntityExists(uid)) continue;
                    list.Add(uid);
                }
            }
        }

        return list;
    }
}
