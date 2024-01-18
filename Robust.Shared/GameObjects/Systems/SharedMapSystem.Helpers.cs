using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedMapSystem
{
    /// <summary>
    /// Converts a tileRef to EntityCoordinates.
    /// </summary>
    public EntityCoordinates ToCoordinates(TileRef tileRef, MapGridComponent? gridComponent = null)
    {
        return ToCoordinates(tileRef.GridUid, tileRef.GridIndices, gridComponent);
    }

    /// <summary>
    /// Converts a tileRef to EntityCoordinates.
    /// </summary>
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
    public EntityCoordinates ToCenterCoordinates(TileRef tileRef, MapGridComponent? gridComponent = null)
    {
        return ToCenterCoordinates(tileRef.GridUid, tileRef.GridIndices, gridComponent);
    }

    /// <summary>
    /// Converts a tileRef to EntityCoordinates for the center of the tile.
    /// </summary>
    public EntityCoordinates ToCenterCoordinates(EntityUid gridUid, Vector2i tile, MapGridComponent? gridComponent = null)
    {
        if (!_gridQuery.Resolve(gridUid, ref gridComponent))
        {
            return EntityCoordinates.Invalid;
        }

        return new EntityCoordinates(gridUid, tile * gridComponent.TileSize + gridComponent.TileSizeHalfVector);
    }
}
