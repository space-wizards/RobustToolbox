using Robust.Shared.Interfaces.GameObjects;
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

        public static EntityCoordinates AlignWithClosestGridTile(this EntityCoordinates coordinates, float searchBoxSize = 1.5f, IEntityManager? entityManager = null, IMapManager? mapManager = null)
        {
            var coords = coordinates;
            entityManager ??= IoCManager.Resolve<IEntityManager>();
            mapManager ??= IoCManager.Resolve<IMapManager>();

            var gridId = coords.GetGridId(entityManager);

            if (!gridId.IsValid() || !mapManager.GridExists(gridId))
            {
                // create a box around the cursor
                var gridSearchBox = Box2.UnitCentered.Scale(searchBoxSize).Translated(coords.Position);

                // find grids in search box
                var gridsInArea = mapManager.FindGridsIntersecting(coords.GetMapId(entityManager), gridSearchBox);

                // find closest grid intersecting our search box.
                IMapGrid? closest = null;
                var distance = float.PositiveInfinity;
                var intersect = new Box2();
                foreach (var grid in gridsInArea)
                {
                    // figure out closest intersect
                    var gridIntersect = gridSearchBox.Intersect(grid.WorldBounds);
                    var gridDist = (gridIntersect.Center - coords.Position).LengthSquared;

                    if (gridDist >= distance)
                        continue;

                    distance = gridDist;
                    closest = grid;
                    intersect = gridIntersect;
                }

                if (closest != null) // stick to existing grid
                {
                    // round to nearest cardinal dir
                    var normal = new Angle(coords.Position - intersect.Center).GetCardinalDir().ToVec();

                    // round coords to center of tile
                    var tileIndices = closest.WorldToTile(intersect.Center);
                    var tileCenterWorld = closest.GridTileToWorldPos(tileIndices);

                    // move mouse one tile out along normal
                    var newTilePos = tileCenterWorld + normal * closest.TileSize;

                    coords = new EntityCoordinates(closest.GridEntityId, closest.WorldToLocal(newTilePos));
                }
                //else free place
            }

            return coords;
        }
    }
}
