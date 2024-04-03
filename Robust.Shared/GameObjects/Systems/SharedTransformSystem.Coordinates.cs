using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Robust.Shared.Map;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedTransformSystem
{
    /*
     * Helper methods for working with EntityCoordinates / MapCoordinates.
     * For grid methods see SharedMapSystem.Coordinates
     */

    /// <summary>
    ///     Verifies that this set of coordinates can be currently resolved to a location.
    /// </summary>
    /// <returns><see langword="true" /> if this set of coordinates can be currently resolved to a location, otherwise <see langword="false" />.</returns>
    public bool IsValid(EntityCoordinates coordinates)
    {
        var entity = coordinates.EntityId;

        if (!entity.IsValid() || !Exists(entity))
            return false;

        if (!float.IsFinite(coordinates.Position.X) || !float.IsFinite(coordinates.Position.Y))
            return false;

        return true;
    }

    /// <summary>
    ///     Returns a new set of EntityCoordinates local to a new entity.
    /// </summary>
    /// <param name="entity">The entity that the new coordinates will be local to</param>
    /// <returns>A new set of EntityCoordinates local to a new entity.</returns>
    public EntityCoordinates WithEntityId(EntityCoordinates coordinates, EntityUid entity)
    {
        var mapPos = ToMapCoordinates(coordinates);

        // You'd think this would throw like ToCoordinates does but TODO check that.
        if (mapPos.MapId == MapId.Nullspace)
        {
            return new EntityCoordinates(entity, Vector2.Zero);
        }

        var xform = XformQuery.GetComponent(entity);

        if (xform.MapID != mapPos.MapId)
        {
            return new EntityCoordinates(entity, Vector2.Zero);
        }

        var localPos = GetInvWorldMatrix(xform).Transform(mapPos.Position);
        return new EntityCoordinates(entity, localPos);
    }

    /// <summary>
    /// Converts entity-local coordinates into map terms.
    /// </summary>
    public MapCoordinates ToMapCoordinates(EntityCoordinates coordinates)
    {
        if (!IsValid(coordinates))
            return MapCoordinates.Nullspace;

        var xform = XformQuery.GetComponent(coordinates.EntityId);
        var worldPos = GetWorldMatrix(xform).Transform(coordinates.Position);
        return new MapCoordinates(worldPos, xform.MapID);
    }

    /// <summary>
    /// Converts entity-local coordinates into map terms.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MapCoordinates ToMapCoordinates(NetCoordinates coordinates)
    {
        var eCoords = GetCoordinates(coordinates);
        return ToMapCoordinates(eCoords);
    }

    /// <summary>
    ///    Creates EntityCoordinates given an entity and some MapCoordinates.
    /// </summary>
    /// <exception cref="InvalidOperationException">If <see cref="entity"/> is not on the same map as the <see cref="coordinates"/>.</exception>
    public EntityCoordinates ToCoordinates(EntityUid entity, MapCoordinates coordinates)
    {
        var xform = XformQuery.GetComponent(entity);

        if (xform.MapID != coordinates.MapId)
            throw new InvalidOperationException("Entity is not on the same map!");

        var localPos = GetInvWorldMatrix(xform).Transform(coordinates.Position);
        return new EntityCoordinates(entity, localPos);
    }
}
