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
        return entity == coordinates.EntityId
            ? coordinates
            : ToCoordinates(entity, ToMapCoordinates(coordinates));
    }

    /// <summary>
    /// Converts entity-local coordinates into map terms.
    /// </summary>
    public MapCoordinates ToMapCoordinates(EntityCoordinates coordinates, bool logError = true)
    {
        if (!TryComp(coordinates.EntityId, out TransformComponent? xform))
        {
            if (logError)
                Log.Error($"Attempted to convert coordinates with invalid entity: {coordinates}. Trace: {Environment.StackTrace}");
            return MapCoordinates.Nullspace;
        }

        var worldPos = Vector2.Transform(coordinates.Position, GetWorldMatrix(xform));
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
            Log.Error($"Attempted to convert coordinates with invalid entity: {coordinates}. Trace: {Environment.StackTrace}");
            return default;
        }

        if (entity.Comp.MapID != coordinates.MapId)
        {
            Log.Error($"Attempted to convert map coordinates {coordinates} to entity coordinates on a different map. Entity: {ToPrettyString(entity)}. Trace: {Environment.StackTrace}");
            return default;
        }

        var localPos = Vector2.Transform(coordinates.Position, GetInvWorldMatrix(entity.Comp));
        return new EntityCoordinates(entity, localPos);
    }

    /// <summary>
    /// Creates map-relative <see cref="EntityCoordinates"/> given some <see cref="MapCoordinates"/>.
    /// </summary>
    public EntityCoordinates ToCoordinates(MapCoordinates coordinates)
    {
        if (_map.TryGetMap(coordinates.MapId, out var uid))
            return ToCoordinates(uid.Value, coordinates);

        Log.Error($"Attempted to convert map coordinates with unknown map id: {coordinates}. Trace: {Environment.StackTrace}");
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
        return !Resolve(entity, ref entity.Comp, logMissing:false) ? null : entity.Comp.GridUid;
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
        return !Resolve(entity, ref entity.Comp, logMissing: false) ? MapId.Nullspace : entity.Comp.MapID;
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
        return !Resolve(entity, ref entity.Comp, logMissing: false) ? null : entity.Comp.MapUid;
    }

    /// <summary>
    ///     Compares two sets of coordinates to see if they are in range of each other.
    /// </summary>
    /// <param name="range">maximum distance between the two sets of coordinates.</param>
    /// <returns>True if the two points are within a given range.</returns>
    public bool InRange(EntityCoordinates coordA, EntityCoordinates coordB,  float range)
    {
        if (!coordA.EntityId.IsValid() || !coordB.EntityId.IsValid())
            return false;

        if (coordA.EntityId == coordB.EntityId)
            return (coordA.Position - coordB.Position).LengthSquared() < range * range;

        var mapA = ToMapCoordinates(coordA, logError:false);
        var mapB = ToMapCoordinates(coordB, logError:false);

        if (mapA.MapId != mapB.MapId || mapA.MapId == MapId.Nullspace)
            return false;

        return mapA.InRange(mapB, range);
    }

    /// <summary>
    ///     Compares the positions of two entities to see if they are within some specified distance of each other.
    /// </summary>
    public bool InRange(Entity<TransformComponent?> entA, Entity<TransformComponent?> entB,  float range)
    {
        if (!Resolve(entA, ref entA.Comp, logMissing: false))
            return false;

        if (!Resolve(entB, ref entB.Comp, logMissing: false))
            return false;

        if (!entA.Comp.ParentUid.IsValid() || !entB.Comp.ParentUid.IsValid())
            return false;

        if (entA.Comp.ParentUid == entB.Comp.ParentUid)
            return (entA.Comp.LocalPosition - entB.Comp.LocalPosition).LengthSquared() < range * range;

        if (entA.Comp.ParentUid == entB.Owner)
            return entA.Comp.LocalPosition.LengthSquared() < range * range;

        if (entB.Comp.ParentUid == entA.Owner)
            return entB.Comp.LocalPosition.LengthSquared() < range * range;

        var mapA = GetMapCoordinates(entA!);
        var mapB = GetMapCoordinates(entB!);

        if (mapA.MapId != mapB.MapId || mapA.MapId == MapId.Nullspace)
            return false;

        return mapA.InRange(mapB, range);
    }
}
