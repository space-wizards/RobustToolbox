using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedMapSystem
{
    /// <summary>
    /// If the supplied coordinates intersects a grid will align with the tile center, otherwise returns the coordinates.
    /// </summary>
    /// <param name="coordinates"></param>
    /// <returns></returns>
    [Pure]
    public EntityCoordinates AlignToGrid(EntityCoordinates coordinates)
    {
        // Check if the parent is already a grid.
        if (_gridQuery.TryGetComponent(coordinates.EntityId, out var gridComponent))
        {
            var tile = CoordinatesToTile(coordinates.EntityId, gridComponent, coordinates);
            return ToCenterCoordinates(coordinates.EntityId, tile, gridComponent);
        }

        // Check if mappos intersects a grid.
        var mapPos = _transform.ToMapCoordinates(coordinates);

        if (_mapInternal.TryFindGridAt(mapPos, out var gridUid, out gridComponent))
        {
            var tile = CoordinatesToTile(gridUid, gridComponent, coordinates);
            return ToCenterCoordinates(gridUid, tile, gridComponent);
        }

        // No grid so just return it.
        return coordinates;
    }

    /// <summary>
    /// Converts a tileRef to EntityCoordinates.
    /// </summary>
    [Pure]
    public EntityCoordinates ToCoordinates(TileRef tileRef, MapGridComponent? gridComponent = null)
    {
        return ToCoordinates(tileRef.GridUid, tileRef.GridIndices, gridComponent);
    }

    /// <summary>
    /// Converts a tileRef to EntityCoordinates.
    /// </summary>
    [Pure]
    public EntityCoordinates ToCoordinates(EntityUid gridUid, Vector2i tile, MapGridComponent? gridComponent = null)
    {
        if (!_gridQuery.Resolve(gridUid, ref gridComponent))
        {
            return EntityCoordinates.Invalid;
        }

        return new EntityCoordinates(gridUid, tile * gridComponent.TileSize);
    }

    /// <summary>
    /// Converts a tileRef to EntityCoordinates for the center of the tile.
    /// </summary>
    [Pure]
    public EntityCoordinates ToCenterCoordinates(TileRef tileRef, MapGridComponent? gridComponent = null)
    {
        return ToCenterCoordinates(tileRef.GridUid, tileRef.GridIndices, gridComponent);
    }

    /// <summary>
    /// Converts a tileRef to EntityCoordinates for the center of the tile.
    /// </summary>
    [Pure]
    public EntityCoordinates ToCenterCoordinates(EntityUid gridUid, Vector2i tile, MapGridComponent? gridComponent = null)
    {
        if (!_gridQuery.Resolve(gridUid, ref gridComponent))
        {
            return EntityCoordinates.Invalid;
        }

        return new EntityCoordinates(gridUid, tile * gridComponent.TileSize + gridComponent.TileSizeHalfVector);
    }
}
