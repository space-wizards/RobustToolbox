using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Shared.Map
{
    public static class CoordinatesExtensions
    {
        public static EntityCoordinates ToEntityCoordinates(this Vector2i vector, GridId gridId, IMapManager? mapManager = null)
        {
            mapManager ??= IoCManager.Resolve<IMapManager>();

            var grid = mapManager.GetGrid(gridId);
            var tile = grid.TileSize;
            
            return new EntityCoordinates(grid.GridEntityId, (vector.X * tile, vector.Y * tile));
        }
    }
}