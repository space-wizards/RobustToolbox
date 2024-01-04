using System.Collections.Generic;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects;

public sealed partial class EntityLookupSystem
{
    /*
     * Local AABB / Box2Rotated queries for broadphase entities.
     */

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
        AddLocalEntitiesIntersecting(gridUid, intersecting, localAABB, flags);
        AddContained(intersecting, flags);
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
}
