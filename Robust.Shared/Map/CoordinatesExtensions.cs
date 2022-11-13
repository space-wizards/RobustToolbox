using System;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Robust.Shared.Map
{
    public static class CoordinatesExtensions
    {
        public static EntityCoordinates ToEntityCoordinates(this Vector2i vector, EntityUid gridId, IMapManager? mapManager = null)
        {
            IoCManager.Resolve(ref mapManager);

            var grid = mapManager.GetGrid(gridId);
            var tile = grid.TileSize;

            return new EntityCoordinates(grid.GridEntityId, (vector.X * tile, vector.Y * tile));
        }

        public static EntityCoordinates AlignWithClosestGridTile(this EntityCoordinates coords, float searchBoxSize = 1.5f, IEntityManager? entityManager = null, IMapManager? mapManager = null)
        {
            IoCManager.Resolve(ref entityManager, ref mapManager);

            var gridId = coords.GetGridUid(entityManager);

            if (mapManager.TryGetGrid(gridId, out var mapGrid))
            {
                return mapGrid.GridTileToLocal(mapGrid.CoordinatesToTile(coords));
            }
            else
            {
                var mapCoords = coords.ToMap(entityManager);

                if (mapManager.TryFindGridAt(mapCoords, out mapGrid))
                {
                    return mapGrid.GridTileToLocal(mapGrid.CoordinatesToTile(coords));
                }

                // create a box around the cursor
                var gridSearchBox = Box2.UnitCentered.Scale(searchBoxSize).Translated(mapCoords.Position);

                // find grids in search box
                var gridsInArea = mapManager.FindGridsIntersecting(mapCoords.MapId, gridSearchBox);

                // find closest grid intersecting our search box.
                MapGridComponent? closest = null;
                var distance = float.PositiveInfinity;
                var intersect = new Box2();
                var xformQuery = entityManager.GetEntityQuery<TransformComponent>();

                foreach (var grid in gridsInArea)
                {
                    var gridXform = xformQuery.GetComponent(grid.GridEntityId);
                    // TODO: Use CollisionManager to get nearest edge.

                    // figure out closest intersect
                    var gridIntersect = gridSearchBox.Intersect(gridXform.WorldMatrix.TransformBox(grid.LocalAABB));
                    var gridDist = (gridIntersect.Center - mapCoords.Position).LengthSquared;

                    if (gridDist >= distance)
                        continue;

                    distance = gridDist;
                    closest = grid;
                    intersect = gridIntersect;
                }

                if (closest != null) // stick to existing grid
                {
                    // round to nearest cardinal dir
                    var normal = mapCoords.Position - intersect.Center;

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
