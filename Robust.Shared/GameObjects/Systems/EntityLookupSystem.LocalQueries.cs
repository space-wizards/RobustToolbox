using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Shared.GameObjects;

public sealed partial class EntityLookupSystem
{
    /*
     * Local AABB / Box2Rotated queries for broadphase entities.
     */

    private void AddLocalEntitiesIntersecting(
        EntityUid lookupUid,
        HashSet<EntityUid> intersecting,
        Box2 localAABB,
        LookupFlags flags,
        BroadphaseComponent? lookup = null)
    {
        if (!_broadQuery.Resolve(lookupUid, ref lookup))
            return;

        var lookupPoly = new PolygonShape();
        lookupPoly.SetAsBox(localAABB);
        var (lookupPos, lookupRot) = _transform.GetWorldPositionRotation(lookupUid);
        var lookupTransform = new Transform(lookupPos, lookupRot);

        AddEntitiesIntersecting(lookupUid, intersecting, lookupPoly, lookupTransform, flags, lookup);
    }

    private void AddLocalEntitiesIntersecting(
        EntityUid lookupUid,
        HashSet<EntityUid> intersecting,
        Box2Rotated localBounds,
        LookupFlags flags,
        BroadphaseComponent? lookup = null)
    {
        if (!_broadQuery.Resolve(lookupUid, ref lookup))
            return;

        var shape = new PolygonShape();
        shape.Set(localBounds);

        var transform = _physics.GetPhysicsTransform(lookupUid);
        AddEntitiesIntersecting(lookupUid, intersecting, shape, transform, flags);
    }

    public bool AnyLocalEntitiesIntersecting(EntityUid lookupUid,
        Box2 localAABB,
        LookupFlags flags,
        EntityUid? ignored = null,
        BroadphaseComponent? lookup = null)
    {
        if (!_broadQuery.Resolve(lookupUid, ref lookup))
            return false;

        var shape = new PolygonShape();
        shape.SetAsBox(localAABB);
        var transform = _physics.GetPhysicsTransform(lookupUid);
        return AnyEntitiesIntersecting(lookupUid, shape, transform, flags, ignored, lookup);
    }

    public HashSet<EntityUid> GetLocalEntitiesIntersecting(EntityUid gridId, Vector2i gridIndices, float enlargement = TileEnlargementRadius, LookupFlags flags = DefaultFlags, MapGridComponent? gridComp = null)
    {
        // Technically this doesn't consider anything overlapping from outside the grid but is this an issue?
        var intersecting = new HashSet<EntityUid>();

        GetLocalEntitiesIntersecting(gridId, gridIndices, intersecting, enlargement, flags, gridComp);
        return intersecting;
    }

    /// <summary>
    /// Gets the entities intersecting the specified broadphase entity using a local AABB.
    /// </summary>
    public void GetLocalEntitiesIntersecting(EntityUid gridUid, Vector2i localTile, HashSet<EntityUid> intersecting,
        float enlargement = TileEnlargementRadius, LookupFlags flags = DefaultFlags, MapGridComponent? gridComp = null)
    {
        ushort tileSize = 1;

        if (_gridQuery.Resolve(gridUid, ref gridComp))
        {
            tileSize = gridComp.TileSize;
        }

        var localAABB = GetLocalBounds(localTile, tileSize);
        localAABB = localAABB.Enlarged(TileEnlargementRadius);
        GetLocalEntitiesIntersecting(gridUid, localAABB, intersecting, flags);
    }

    /// <summary>
    /// Gets the entities intersecting the specified broadphase entity using a local AABB.
    /// </summary>
    public void GetLocalEntitiesIntersecting(EntityUid gridUid, Box2 localAABB, HashSet<EntityUid> intersecting,
        LookupFlags flags = DefaultFlags)
    {
        AddLocalEntitiesIntersecting(gridUid, intersecting, localAABB, flags);
        AddContained(intersecting, flags);
    }

    /// <summary>
    /// Gets the entities intersecting the specified broadphase entity using a local Box2Rotated.
    /// </summary>
    public void GetLocalEntitiesIntersecting(EntityUid gridUid, Box2Rotated localBounds, HashSet<EntityUid> intersecting,
        LookupFlags flags = DefaultFlags)
    {
        AddLocalEntitiesIntersecting(gridUid, intersecting, localBounds, flags);
        AddContained(intersecting, flags);
    }

    /// <summary>
    /// Returns the entities intersecting any of the supplied tiles. Faster than doing each tile individually.
    /// </summary>
    public HashSet<EntityUid> GetLocalEntitiesIntersecting(EntityUid gridId, IEnumerable<Vector2i> gridIndices, LookupFlags flags = DefaultFlags)
    {
        var intersecting = new HashSet<EntityUid>();

        // Technically this doesn't consider anything overlapping from outside the grid but is this an issue?
        if (!_gridQuery.TryGetComponent(gridId, out var mapGrid))
            return intersecting;

        // TODO: You can probably decompose the indices into larger areas if you take in a hashset instead.
        foreach (var index in gridIndices)
        {
            GetLocalEntitiesIntersecting(gridId, index, intersecting, flags: flags, gridComp: mapGrid);
        }

        return intersecting;
    }

    public HashSet<EntityUid> GetLocalEntitiesIntersecting(BroadphaseComponent lookup, Box2 localAABB, LookupFlags flags = DefaultFlags)
    {
        var intersecting = new HashSet<EntityUid>();

        AddLocalEntitiesIntersecting(lookup.Owner, intersecting, localAABB, flags, lookup);
        AddContained(intersecting, flags);

        return intersecting;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<EntityUid> GetLocalEntitiesIntersecting(TileRef tileRef, float enlargement = TileEnlargementRadius, LookupFlags flags = DefaultFlags)
    {
        return GetLocalEntitiesIntersecting(tileRef.GridUid, tileRef.GridIndices, enlargement, flags);
    }
}
