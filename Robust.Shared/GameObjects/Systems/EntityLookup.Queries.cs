using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public sealed partial class EntityLookupSystem
{
    /*
     * There is no GetEntitiesInMap method as this should be avoided; anyone that really needs it can implement it themselves
     */

    // Internal API messy for now but mainly want external to be fairly stable for a while and optimise it later.

    #region Private

    private void RecursiveAdd(EntityUid uid, ref ValueList<EntityUid> toAdd)
    {
        if (!_xformQuery.TryGetComponent(uid, out var xform))
        {
            Log.Error($"Encountered deleted entity {uid} while performing entity lookup.");
            return;
        }

        toAdd.Add(uid);
        foreach (var child in xform._children)
        {
            RecursiveAdd(child, ref toAdd);
        }
    }

    private void AddContained(HashSet<EntityUid> intersecting, LookupFlags flags)
    {
        if ((flags & LookupFlags.Contained) == 0x0 || intersecting.Count == 0)
            return;

        // TODO PERFORMANCE.
        // toAdd only exists because we can't add directly to intersecting w/o enumeration issues.
        // If we assume that there are more entities in containers than there are entities in the intersecting set, then
        // we would be better off creating a fixed-size EntityUid array and coping all intersecting entities into that
        // instead of creating a value list here that needs to be resized.
        var toAdd = new ValueList<EntityUid>();

        foreach (var uid in intersecting)
        {
            if (!_containerQuery.TryGetComponent(uid, out var conManager))
                continue;

            foreach (var con in _container.GetAllContainers(uid, conManager))
            {
                foreach (var contained in con.ContainedEntities)
                {
                    RecursiveAdd(contained, ref toAdd);
                }
            }
        }

        foreach (var uid in toAdd)
        {
            intersecting.Add(uid);
        }
    }

    /// <summary>
    /// Wrapper around the per-grid version.
    /// </summary>
    private void AddEntitiesIntersecting(MapId mapId,
        HashSet<EntityUid> intersecting,
        IPhysShape shape,
        Transform shapeTransform,
        LookupFlags flags)
    {
        var worldAABB = shape.ComputeAABB(shapeTransform, 0);
        var state = new EntityQueryState(intersecting,
            shape,
            shapeTransform,
            _fixtures,
            this,
            _physics,
            _manifoldManager,
            _fixturesQuery,
            flags);

        // Need to include maps
        _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
            static (EntityUid uid, MapGridComponent _, ref EntityQueryState state) =>
            {
                state.Lookup.AddEntitiesIntersecting(uid, state.Intersecting, state.Shape, state.Transform, state.Flags);
                return true;
            }, approx: true, includeMap: false);

        var mapUid = _mapManager.GetMapEntityId(mapId);
        AddEntitiesIntersecting(mapUid, intersecting, shape, shapeTransform, flags);
        AddContained(intersecting, flags);
    }

    private void AddEntitiesIntersecting(
        EntityUid lookupUid,
        HashSet<EntityUid> intersecting,
        IPhysShape shape,
        Transform shapeTransform,
        LookupFlags flags,
        BroadphaseComponent? lookup = null)
    {
        if (!_broadQuery.Resolve(lookupUid, ref lookup))
            return;

        var (_, lookupRot, lookupInvMatrix) = _transform.GetWorldPositionRotationInvMatrix(lookupUid);
        var lookupTransform = new Transform(lookupInvMatrix.Transform(shapeTransform.Position),
            shapeTransform.Quaternion2D.Angle - lookupRot);

        var localAABB = shape.ComputeAABB(lookupTransform, 0);

        var state = new EntityQueryState(
            intersecting,
            shape,
            shapeTransform,
            _fixtures,
            this,
            _physics,
            _manifoldManager,
            _fixturesQuery,
            flags);

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref state, PhysicsQuery, localAABB, true);
        }

        if ((flags & LookupFlags.Static) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref state, PhysicsQuery, localAABB, true);
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            lookup.StaticSundriesTree.QueryAabb(ref state, SundriesQuery, localAABB, true);
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            lookup.SundriesTree.QueryAabb(ref state, SundriesQuery, localAABB, true);
        }

        return;

        static bool PhysicsQuery(ref EntityQueryState state, in FixtureProxy value)
        {
            var sensors = (state.Flags & LookupFlags.Sensors) != 0x0;

            if (!sensors && !value.Fixture.Hard)
                return true;

            var approx = (state.Flags & LookupFlags.Approximate) != 0x0;

            if (!approx)
            {
                var intersectingTransform = state.Physics.GetPhysicsTransform(value.Entity);
                if (!state.Manifolds.TestOverlap(state.Shape, 0, value.Fixture.Shape, value.ChildIndex, state.Transform, intersectingTransform))
                {
                    return true;
                }
            }

            state.Intersecting.Add(value.Entity);
            return true;
        }

        static bool SundriesQuery(ref EntityQueryState state, in EntityUid value)
        {
            var approx = (state.Flags & LookupFlags.Approximate) != 0x0;

            if (approx)
            {
                state.Intersecting.Add(value);
                return true;
            }

            var intersectingTransform = state.Physics.GetPhysicsTransform(value);

            if (state.FixturesQuery.TryGetComponent(value, out var fixtures))
            {
                var sensors = (state.Flags & LookupFlags.Sensors) != 0x0;
                bool anyFixture = false;

                foreach (var fixture in fixtures.Fixtures.Values)
                {
                    if (!sensors && !fixture.Hard)
                        continue;

                    anyFixture = true;
                    for (var i = 0; i < fixture.Shape.ChildCount; i++)
                    {
                        if (state.Manifolds.TestOverlap(state.Shape, 0, fixture.Shape, i, state.Transform,
                                intersectingTransform))
                        {
                            state.Intersecting.Add(value);
                            return true;
                        }
                    }
                }

                if (anyFixture)
                    return true;
            }

            if (state.Fixtures.TestPoint(state.Shape, state.Transform, intersectingTransform.Position))
                state.Intersecting.Add(value);

            return true;
        }
    }

    /// <summary>
    /// Wrapper around the per-grid version.
    /// </summary>
    private bool AnyEntitiesIntersecting(MapId mapId,
        IPhysShape shape,
        Transform shapeTransform,
        LookupFlags flags,
        EntityUid? ignored = null)
    {
        var worldAABB = shape.ComputeAABB(shapeTransform, 0);
        var state = new AnyEntityQueryState(false,
            ignored,
            shape,
            shapeTransform,
            _fixtures,
            this,
            _physics,
            _manifoldManager,
            _fixturesQuery,
            flags);

        // Need to include maps
        _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
            static (EntityUid uid, MapGridComponent _, ref AnyEntityQueryState state) =>
            {
                if (state.Lookup.AnyEntitiesIntersecting(uid, state.Shape, state.Transform, state.Flags, ignored: state.Ignored))
                {
                    state.Found = true;
                    return false;
                }

                return true;
            }, approx: true, includeMap: false);

        if (!state.Found)
        {
            var mapUid = _mapManager.GetMapEntityId(mapId);
            state.Found = AnyEntitiesIntersecting(mapUid, shape, shapeTransform, flags, ignored);
        }

        return state.Found;
    }

    private bool AnyEntitiesIntersecting(EntityUid lookupUid,
        IPhysShape shape,
        Transform shapeTransform,
        LookupFlags flags,
        EntityUid? ignored = null,
        BroadphaseComponent? lookup = null)
    {
        if (!_broadQuery.Resolve(lookupUid, ref lookup))
            return false;

        var state = new AnyEntityQueryState(false,
            ignored,
            shape,
            shapeTransform,
            _fixtures,
            this,
            _physics,
            _manifoldManager,
            _fixturesQuery,
            flags);

        // Shape gets passed in via local terms
        // Transform is in world terms
        // Need to convert both back to lookup-local for AABB.
        var (_, lookupRot, lookupInvMatrix) = _transform.GetWorldPositionRotationInvMatrix(lookupUid);
        var lookupTransform = new Transform(lookupInvMatrix.Transform(shapeTransform.Position),
            shapeTransform.Quaternion2D.Angle - lookupRot);

        var localAABB = shape.ComputeAABB(lookupTransform, 0);

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref state, PhysicsQuery, localAABB, true);

            if (state.Found)
                return true;
        }

        if ((flags & LookupFlags.Static) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref state, PhysicsQuery, localAABB, true);

            if (state.Found)
                return true;
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            lookup.StaticSundriesTree.QueryAabb(ref state, SundriesQuery, localAABB, true);

            if (state.Found)
                return true;
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            lookup.SundriesTree.QueryAabb(ref state, SundriesQuery, localAABB, true);
        }

        return state.Found;

        static bool PhysicsQuery(ref AnyEntityQueryState state, in FixtureProxy value)
        {
            if (state.Ignored == value.Entity)
                return true;

            var sensors = (state.Flags & LookupFlags.Sensors) != 0x0;

            if (!sensors && !value.Fixture.Hard)
                return true;

            var approx = (state.Flags & LookupFlags.Approximate) != 0x0;

            if (!approx)
            {
                var intersectingTransform = state.Physics.GetPhysicsTransform(value.Entity);
                if (!state.Manifolds.TestOverlap(state.Shape, 0, value.Fixture.Shape, value.ChildIndex, state.Transform, intersectingTransform))
                {
                    return true;
                }
            }

            state.Found = true;
            return false;
        }

        static bool SundriesQuery(ref AnyEntityQueryState state, in EntityUid value)
        {
            if (state.Ignored == value)
                return true;

            var approx = (state.Flags & LookupFlags.Approximate) != 0x0;

            if (approx)
            {
                state.Found = true;
                return false;
            }

            var intersectingTransform = state.Physics.GetPhysicsTransform(value);

            if (state.FixturesQuery.TryGetComponent(value, out var fixtures))
            {
                var sensors = (state.Flags & LookupFlags.Sensors) != 0x0;
                bool anyFixture = false;

                foreach (var fixture in fixtures.Fixtures.Values)
                {
                    if (!sensors && !fixture.Hard)
                        continue;

                    anyFixture = true;
                    for (var i = 0; i < fixture.Shape.ChildCount; i++)
                    {
                        if (state.Manifolds.TestOverlap(state.Shape, 0, fixture.Shape, i, state.Transform,
                                intersectingTransform))
                        {
                            state.Found = true;
                            return false;
                        }
                    }
                }

                if (anyFixture)
                    return true;
            }

            if (state.Fixtures.TestPoint(state.Shape, state.Transform, intersectingTransform.Position))
            {
                state.Found = true;
                return false;
            }

            return true;
        }
    }

    private bool AnyEntitiesIntersecting(EntityUid lookupUid,
        Box2Rotated worldBounds,
        LookupFlags flags,
        EntityUid? ignored = null)
    {
        var shape = new PolygonShape();
        shape.Set(worldBounds);
        var transform = Physics.Transform.Empty;

        return AnyEntitiesIntersecting(lookupUid, shape, transform, flags, ignored);
    }

    #endregion

    #region Arc

    public IEnumerable<EntityUid> GetEntitiesInArc(
        EntityCoordinates coordinates,
        float range,
        Angle direction,
        float arcWidth,
        LookupFlags flags = DefaultFlags)
    {
        var position = coordinates.ToMap(EntityManager, _transform);

        return GetEntitiesInArc(position, range, direction, arcWidth, flags);
    }

    public IEnumerable<EntityUid> GetEntitiesInArc(
        MapCoordinates coordinates,
        float range,
        Angle direction,
        float arcWidth,
        LookupFlags flags = DefaultFlags)
    {
        foreach (var entity in GetEntitiesInRange(coordinates, range * 2, flags))
        {
            var angle = new Angle(_transform.GetWorldPosition(entity) - coordinates.Position);
            if (angle.Degrees < direction.Degrees + arcWidth / 2 &&
                angle.Degrees > direction.Degrees - arcWidth / 2)
                yield return entity;
        }
    }

    #endregion

    #region Box2

    public bool AnyEntitiesIntersecting(MapId mapId, Box2 worldAABB, LookupFlags flags = DefaultFlags)
    {
        if (mapId == MapId.Nullspace) return false;

        // Don't need to check contained entities as they have the same bounds as the parent.
        var found = false;

        var state = (this, worldAABB, flags, found);

        _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
            static (EntityUid uid, MapGridComponent _, ref (EntityLookupSystem lookup, Box2 worldAABB, LookupFlags flags, bool found) tuple) =>
            {
                if (!tuple.lookup.AnyLocalEntitiesIntersecting(uid, tuple.worldAABB, tuple.flags))
                    return true;

                tuple.found = true;
                return false;
            }, approx: true, includeMap: false);

        if (state.found)
            return true;

        var mapUid = _mapManager.GetMapEntityId(mapId);
        return AnyLocalEntitiesIntersecting(mapUid, worldAABB, flags);
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2 worldAABB, LookupFlags flags = DefaultFlags)
    {
        var intersecting = new HashSet<EntityUid>();
        GetEntitiesIntersecting(mapId, worldAABB, intersecting, flags);
        return intersecting;
    }

    public void GetEntitiesIntersecting(MapId mapId, Box2 worldAABB, HashSet<EntityUid> intersecting, LookupFlags flags = DefaultFlags)
    {
        if (mapId == MapId.Nullspace) return;

        // Get grid entities
        var state = (this, intersecting, worldAABB, _transform, flags);

        _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
            static (EntityUid gridUid, MapGridComponent _, ref (
                EntityLookupSystem lookup, HashSet<EntityUid> intersecting,
                Box2 worldAABB, SharedTransformSystem xformSystem, LookupFlags flags) tuple) =>
            {
                var localAABB = tuple.xformSystem.GetInvWorldMatrix(gridUid).TransformBox(tuple.worldAABB);
                tuple.lookup.AddLocalEntitiesIntersecting(gridUid, tuple.intersecting, localAABB, tuple.flags);
                return true;
            }, approx: true, includeMap: false);

        // Get map entities
        var mapUid = _mapManager.GetMapEntityId(mapId);
        AddLocalEntitiesIntersecting(mapUid, intersecting, worldAABB, flags);
        AddContained(intersecting, flags);
    }

    #endregion

    #region Box2Rotated

    public bool AnyEntitiesIntersecting(MapId mapId, Box2Rotated worldBounds, LookupFlags flags = DefaultFlags)
    {
        // Don't need to check contained entities as they have the same bounds as the parent.
        var worldAABB = worldBounds.CalcBoundingBox();

        const bool found = false;
        var state = (this, worldBounds, flags, found);

        _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
            static (EntityUid uid, MapGridComponent grid, ref (EntityLookupSystem lookup, Box2Rotated worldBounds, LookupFlags flags, bool found) tuple) =>
            {
                if (tuple.lookup.AnyEntitiesIntersecting(uid, tuple.worldBounds, tuple.flags))
                {
                    tuple.found = true;
                    return false;
                }
                return true;
            }, approx: true, includeMap: false);

        if (state.found)
            return true;

        var mapUid = _mapManager.GetMapEntityId(mapId);
        return AnyEntitiesIntersecting(mapUid, worldBounds, flags);
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2Rotated worldBounds, LookupFlags flags = DefaultFlags)
    {
        var intersecting = new HashSet<EntityUid>();

        if (mapId == MapId.Nullspace)
            return intersecting;

        var mapUid = _mapManager.GetMapEntityId(mapId);

        // Get grid entities
        var shape = new PolygonShape();
        shape.Set(worldBounds);

        var state = (this, intersecting, shape, flags);

        _mapManager.FindGridsIntersecting(mapUid, shape, Physics.Transform.Empty, ref state, static
        (EntityUid uid, MapGridComponent _,
            ref (EntityLookupSystem lookup,
                HashSet<EntityUid> intersecting,
                PolygonShape shape,
                LookupFlags flags) tuple) =>
        {
            tuple.lookup.AddEntitiesIntersecting(uid, tuple.intersecting, tuple.shape, Physics.Transform.Empty, tuple.flags);
            return true;
        }, approx: true, includeMap: false);

        // Get map entities
        AddEntitiesIntersecting(mapUid, intersecting, shape, Physics.Transform.Empty, flags);
        AddContained(intersecting, flags);

        return intersecting;
    }

    #endregion

    #region Entity

    public bool AnyEntitiesIntersecting(EntityUid uid, LookupFlags flags = DefaultFlags)
    {
        return AnyEntitiesInRange(uid, LookupEpsilon, flags);
    }

    public bool AnyEntitiesInRange(EntityUid uid, float range, LookupFlags flags = DefaultFlags)
    {
        var mapPos = _transform.GetMapCoordinates(uid);

        if (mapPos.MapId == MapId.Nullspace)
            return false;

        var rangeVec = new Vector2(range, range);
        var worldAABB = new Box2(mapPos.Position - rangeVec, mapPos.Position + rangeVec);
        var circle = new PhysShapeCircle(range, mapPos.Position);

        const bool found = false;
        var state = (this, worldAABB, circle, flags, found, uid);

        _mapManager.FindGridsIntersecting(mapPos.MapId, worldAABB, ref state, static (
            EntityUid gridUid,
            MapGridComponent _, ref (
                EntityLookupSystem lookup,
                Box2 worldAABB,
                PhysShapeCircle circle,
                LookupFlags flags,
                bool found,
                EntityUid ignored) tuple) =>
        {
            if (tuple.lookup.AnyEntitiesIntersecting(gridUid, tuple.circle, Physics.Transform.Empty, tuple.flags, tuple.ignored))
            {
                tuple.found = true;
                return false;
            }

            return true;
        }, approx: true, includeMap: false);

        if (state.found)
        {
            return true;
        }

        var mapUid = _mapManager.GetMapEntityId(mapPos.MapId);
        return AnyEntitiesIntersecting(mapUid, circle, Physics.Transform.Empty, flags, uid);
    }

    public HashSet<EntityUid> GetEntitiesInRange(EntityUid uid, float range, LookupFlags flags = DefaultFlags)
    {
        var mapPos = _transform.GetMapCoordinates(uid);

        if (mapPos.MapId == MapId.Nullspace)
            return [];

        var intersecting = GetEntitiesInRange(mapPos, range, flags);
        intersecting.Remove(uid);
        return intersecting;
    }

    public void GetEntitiesInRange(EntityUid uid, float range, HashSet<EntityUid> entities, LookupFlags flags = DefaultFlags)
    {
        var mapPos = _transform.GetMapCoordinates(uid);

        if (mapPos.MapId == MapId.Nullspace)
            return;

        GetEntitiesInRange(mapPos.MapId, mapPos.Position, range, entities, flags);
        entities.Remove(uid);
    }

    public void GetEntitiesIntersecting(EntityUid uid, HashSet<EntityUid> intersecting, LookupFlags flags = DefaultFlags)
    {
        var xform = _xformQuery.GetComponent(uid);
        var mapId = xform.MapID;

        if (mapId == MapId.Nullspace)
            return;

        var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform);
        var worldAABB = GetAABBNoContainer(uid, worldPos, worldRot);
        var existing = intersecting.Contains(uid);
        var transform = new Transform(worldPos, worldRot);
        var state = (uid, transform, intersecting, _fixturesQuery, this, flags);

        // Unfortuantely I can't think of a way to de-dupe this with the other ones as it's slightly different.
        _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
            static (EntityUid gridUid, MapGridComponent grid,
                ref (EntityUid entity, Transform transform, HashSet<EntityUid> intersecting,
                    EntityQuery<FixturesComponent> fixturesQuery, EntityLookupSystem lookup, LookupFlags flags) tuple) =>
            {
                EntityIntersectingQuery(gridUid, tuple);

                return true;
            }, approx: true, includeMap: false);

        var mapUid = _mapManager.GetMapEntityId(mapId);
        EntityIntersectingQuery(mapUid, state);

        // Remove the entity itself (unless it was passed in).
        if (!existing)
        {
            intersecting.Remove(uid);
        }

        return;

        static void EntityIntersectingQuery(EntityUid lookupUid, (EntityUid entity, Transform shapeTransform, HashSet<EntityUid> intersecting,
            EntityQuery<FixturesComponent> fixturesQuery, EntityLookupSystem lookup, LookupFlags flags) tuple)
        {
            if (tuple.fixturesQuery.TryGetComponent(tuple.entity, out var fixturesComp))
            {
                foreach (var fixture in fixturesComp.Fixtures.Values)
                {
                    // If our own fixture isn't hard and sensors ignored then ignore it.
                    if (!fixture.Hard && (tuple.flags & LookupFlags.Sensors) == 0x0)
                        continue;

                    tuple.lookup.AddEntitiesIntersecting(lookupUid, tuple.intersecting, fixture.Shape, tuple.shapeTransform, tuple.flags);
                }
            }
            // Single point check
            else
            {
                var shape = new PhysShapeCircle(LookupEpsilon);
                tuple.lookup.AddEntitiesIntersecting(lookupUid, tuple.intersecting, shape, tuple.shapeTransform, tuple.flags);
            }
        }
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(EntityUid uid, LookupFlags flags = DefaultFlags)
    {
        var xform = _xformQuery.GetComponent(uid);
        var mapId = xform.MapID;
        var intersecting = new HashSet<EntityUid>();

        if (mapId == MapId.Nullspace)
            return intersecting;

        GetEntitiesIntersecting(uid, intersecting, flags);
        return intersecting;
    }

    #endregion

    #region EntityCoordinates

    public bool AnyEntitiesIntersecting(EntityCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        if (!coordinates.IsValid(EntityManager))
            return false;

        var mapPos = coordinates.ToMap(EntityManager, _transform);
        return AnyEntitiesIntersecting(mapPos, flags);
    }

    public bool AnyEntitiesInRange(EntityCoordinates coordinates, float range, LookupFlags flags = DefaultFlags)
    {
        if (!coordinates.IsValid(EntityManager))
            return false;

        var mapPos = coordinates.ToMap(EntityManager, _transform);
        return AnyEntitiesInRange(mapPos, range, flags);
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(EntityCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        var mapPos = coordinates.ToMap(EntityManager, _transform);
        return GetEntitiesIntersecting(mapPos, flags);
    }

    public HashSet<EntityUid> GetEntitiesInRange(EntityCoordinates coordinates, float range, LookupFlags flags = DefaultFlags)
    {
        var ents = new HashSet<EntityUid>();
        GetEntitiesInRange(coordinates, range, ents, flags);
        return ents;
    }

    public void GetEntitiesInRange(EntityCoordinates coordinates, float range, HashSet<EntityUid> entities, LookupFlags flags = DefaultFlags)
    {
        var mapPos = coordinates.ToMap(EntityManager, _transform);

        if (mapPos.MapId == MapId.Nullspace)
            return;

        GetEntitiesInRange(mapPos.MapId, mapPos.Position, range, entities, flags);
    }

    #endregion

    #region MapCoordinates

    public bool AnyEntitiesIntersecting(MapCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        if (coordinates.MapId == MapId.Nullspace)
            return false;

        return AnyEntitiesInRange(coordinates, LookupEpsilon, flags);
    }

    public bool AnyEntitiesInRange(MapCoordinates coordinates, float range, LookupFlags flags = DefaultFlags)
    {
        if (coordinates.MapId == MapId.Nullspace) return false;

        var shape = new PhysShapeCircle(range, coordinates.Position);

        return AnyEntitiesIntersecting(coordinates.MapId, shape, Physics.Transform.Empty, flags);
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(MapCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        if (coordinates.MapId == MapId.Nullspace) return new HashSet<EntityUid>();

        return GetEntitiesInRange(coordinates, LookupEpsilon, flags);
    }

    public HashSet<EntityUid> GetEntitiesInRange(MapCoordinates coordinates, float range, LookupFlags flags = DefaultFlags)
    {
        return GetEntitiesInRange(coordinates.MapId, coordinates.Position, range, flags);
    }

    #endregion

    #region MapId

    public HashSet<EntityUid> GetEntitiesInRange(MapId mapId, Vector2 worldPos, float range,
        LookupFlags flags = DefaultFlags)
    {
        var entities = new HashSet<EntityUid>();
        GetEntitiesInRange(mapId, worldPos, range, entities, flags);
        return entities;
    }

    public void GetEntitiesInRange(MapId mapId, Vector2 worldPos, float range, HashSet<EntityUid> entities, LookupFlags flags = DefaultFlags)
    {
        DebugTools.Assert(range > 0, "Range must be a positive float");

        if (mapId == MapId.Nullspace)
            return;

        var shape = new PhysShapeCircle(range, worldPos);
        AddEntitiesIntersecting(mapId, entities, shape, Physics.Transform.Empty, flags);
    }

    #endregion

    #region Grid Methods

    public HashSet<EntityUid> GetEntitiesIntersecting(EntityUid gridId, Box2 worldAABB, LookupFlags flags = DefaultFlags)
    {
        var intersecting = new HashSet<EntityUid>();
        GetEntitiesIntersecting(gridId, worldAABB, intersecting, flags);
        return intersecting;
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(EntityUid gridId, Box2Rotated worldBounds, LookupFlags flags = DefaultFlags)
    {
        var intersecting = new HashSet<EntityUid>();
        GetEntitiesIntersecting(gridId, worldBounds, intersecting, flags);
        return intersecting;
    }

    public void GetEntitiesIntersecting(EntityUid gridId, Box2 worldAABB, HashSet<EntityUid> intersecting, LookupFlags flags = DefaultFlags)
    {
        if (!_broadQuery.TryGetComponent(gridId, out var lookup))
            return;

        var shape = new PolygonShape();
        shape.SetAsBox(worldAABB);

        AddEntitiesIntersecting(gridId, intersecting, shape, Physics.Transform.Empty, flags, lookup);
        AddContained(intersecting, flags);
    }

    public void GetEntitiesIntersecting(EntityUid gridId, Box2Rotated worldBounds, HashSet<EntityUid> intersecting, LookupFlags flags = DefaultFlags)
    {
        if (!_broadQuery.TryGetComponent(gridId, out var lookup))
            return;

        var shape = new PolygonShape();
        shape.Set(worldBounds);

        AddEntitiesIntersecting(gridId, intersecting, shape, Physics.Transform.Empty, flags, lookup);
        AddContained(intersecting, flags);
    }

    #endregion

    #region Lookups

    /// <summary>
    /// Gets the relevant <see cref="BroadphaseComponent"/> that intersects the specified area.
    /// </summary>
    public void FindLookupsIntersecting(MapId mapId, Box2Rotated worldBounds, ComponentQueryCallback<BroadphaseComponent> callback)
    {
        if (mapId == MapId.Nullspace)
            return;

        var state = (callback, _broadQuery);

        _mapManager.FindGridsIntersecting(mapId, worldBounds, ref state,
            static (EntityUid uid, MapGridComponent grid,
                ref (ComponentQueryCallback<BroadphaseComponent> callback, EntityQuery<BroadphaseComponent> _broadQuery)
                    tuple) =>
            {
                tuple.callback(uid, tuple._broadQuery.GetComponent(uid));
                return true;
            }, approx: true, includeMap: false);

        var mapUid = _mapManager.GetMapEntityId(mapId);
        callback(mapUid, _broadQuery.GetComponent(mapUid));
    }

    #endregion

    #region Bounds

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
        var grid = _gridQuery.GetComponent(tileRef.GridUid);

        if (worldMatrix == null || angle == null)
        {
            var (_, wAng, wMat) = _transform.GetWorldPositionRotationMatrix(tileRef.GridUid);
            worldMatrix = wMat;
            angle = wAng;
        }

        var expand = new Vector2(0.5f, 0.5f);
        var center = worldMatrix.Value.Transform(tileRef.GridIndices + expand) * grid.TileSize;
        var translatedBox = Box2.CenteredAround(center, new Vector2(grid.TileSize, grid.TileSize));

        return new Box2Rotated(translatedBox, -angle.Value, center);
    }

    #endregion

    private record struct AnyEntityQueryState(
        bool Found,
        EntityUid? Ignored,
        IPhysShape Shape,
        Transform Transform,
        FixtureSystem Fixtures,
        EntityLookupSystem Lookup,
        SharedPhysicsSystem Physics,
        IManifoldManager Manifolds,
        EntityQuery<FixturesComponent> FixturesQuery,
        LookupFlags Flags
    );

    private readonly record struct EntityQueryState(
        HashSet<EntityUid> Intersecting,
        IPhysShape Shape,
        Transform Transform,
        FixtureSystem Fixtures,
        EntityLookupSystem Lookup,
        SharedPhysicsSystem Physics,
        IManifoldManager Manifolds,
        EntityQuery<FixturesComponent> FixturesQuery,
        LookupFlags Flags
    );
}
