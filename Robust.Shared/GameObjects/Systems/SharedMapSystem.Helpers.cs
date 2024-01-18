using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedMapSystem
{
    /// <summary>
    /// Converts a tileRef to EntityCoordinates.
    /// </summary>
    public EntityCoordinates ToCoordinates(TileRef tileRef)
    {
        return ToCoordinates(tileRef.GridUid, tileRef.GridIndices);
    }

    /// <summary>
    /// Converts a tileRef to EntityCoordinates.
    /// </summary>
    public EntityCoordinates ToCoordinates(EntityUid gridUid, Vector2i tile)
    {
        if (!_gridQuery.TryGetComponent(gridUid, out var mapGrid))
        {
            return EntityCoordinates.Invalid;
        }

        return new EntityCoordinates(gridUid, tile * mapGrid.TileSize);
    }

    /// <summary>
    /// Converts a tileRef to EntityCoordinates for the center of the tile.
    /// </summary>
    public EntityCoordinates ToCenterCoordinates(TileRef tileRef)
    {
        return ToCenterCoordinates(tileRef.GridUid, tileRef.GridIndices);
    }

    /// <summary>
    /// Converts a tileRef to EntityCoordinates for the center of the tile.
    /// </summary>
    public EntityCoordinates ToCenterCoordinates(EntityUid gridUid, Vector2i tile)
    {
        if (!_gridQuery.TryGetComponent(gridUid, out var mapGrid))
        {
            return EntityCoordinates.Invalid;
        }

        return new EntityCoordinates(gridUid, tile * mapGrid.TileSize + mapGrid.TileSizeHalfVector);
    }
}
