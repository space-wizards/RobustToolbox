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
        return entity == coordinates.EntityId
            ? coordinates
            : ToCoordinates(entity, ToMapCoordinates(coordinates));
    }

    /// <summary>
    /// Converts entity-local coordinates into map terms.
    /// </summary>
    public MapCoordinates ToMapCoordinates(EntityCoordinates coordinates)
    {
        if (!TryComp(coordinates.EntityId, out TransformComponent? xform))
        {
            Log.Error($"Attempted to convert coordinates with invalid entity: {coordinates}");
            return MapCoordinates.Nullspace;
        }

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
    /// Creates EntityCoordinates given an entity and some MapCoordinates.
    /// </summary>
    public EntityCoordinates ToCoordinates(Entity<TransformComponent?> entity, MapCoordinates coordinates)
    {
        if (!Resolve(entity, ref entity.Comp, false))
        {
            Log.Error($"Attempted to convert coordinates with invalid entity: {coordinates}");
            return default;
        }

        if (entity.Comp.MapID != coordinates.MapId)
        {
            Log.Error($"Attempted to convert map coordinates {coordinates} to entity coordinates on a different map. Entity: {ToPrettyString(entity)}");
            return default;
        }

        var localPos = GetInvWorldMatrix(entity.Comp).Transform(coordinates.Position);
        return new EntityCoordinates(entity, localPos);
    }

    /// <summary>
    /// Creates map-relative <see cref="EntityCoordinates"/> given some <see cref="MapCoordinates"/>.
    /// </summary>
    public EntityCoordinates ToCoordinates(MapCoordinates coordinates)
    {
        if (_map.TryGetMap(coordinates.MapId, out var uid))
            return ToCoordinates(uid.Value, coordinates);

        Log.Error($"Attempted to convert map coordinates with unknown map id: {coordinates}");
        return default;

    }

    /// <summary>
    /// Returns the grid that the entity whose position the coordinates are relative to is on.
    /// </summary>
    public EntityUid? GetGrid(EntityCoordinates coordinates)
    {
        return GetGrid(coordinates.EntityId);
    }

    public EntityUid? GetGrid(Entity<TransformComponent?> entity)
    {
        return !Resolve(entity, ref entity.Comp) ? null : entity.Comp.GridUid;
    }

    /// <summary>
    /// Returns the Map Id these coordinates are on.
    /// </summary>
    public MapId GetMapId(EntityCoordinates coordinates)
    {
        return GetMapId(coordinates.EntityId);
    }

    public MapId GetMapId(Entity<TransformComponent?> entity)
    {
        return !Resolve(entity, ref entity.Comp) ? MapId.Nullspace : entity.Comp.MapID;
    }

    /// <summary>
    /// Returns the Map that these coordinates are on.
    /// </summary>
    public EntityUid? GetMap(EntityCoordinates coordinates)
    {
        return GetMap(coordinates.EntityId);
    }

    public EntityUid? GetMap(Entity<TransformComponent?> entity)
    {
        return !Resolve(entity, ref entity.Comp) ? null : entity.Comp.MapUid;
    }

}
