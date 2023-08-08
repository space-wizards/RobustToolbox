using System;
using System.Numerics;
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

            return new EntityCoordinates(gridId, new Vector2(vector.X * tile, vector.Y * tile));
        }

        public static EntityCoordinates AlignWithClosestGridTile(this EntityCoordinates coords, float searchBoxSize = 1.5f, IEntityManager? entityManager = null, IMapManager? mapManager = null)
        {
            IoCManager.Resolve(ref entityManager, ref mapManager);

            var gridId = coords.GetGridUid(entityManager);
            var mapSystem = entityManager.System<SharedMapSystem>();

            if (mapManager.TryGetGrid(gridId, out var mapGrid))
            {
                return mapSystem.GridTileToLocal(gridId.Value, mapGrid, mapSystem.CoordinatesToTile(gridId.Value, mapGrid, coords));
            }

            var mapCoords = coords.ToMap(entityManager);

            if (mapManager.TryFindGridAt(mapCoords, out var gridUid, out mapGrid))
            {
                return mapSystem.GridTileToLocal(gridUid, mapGrid, mapSystem.CoordinatesToTile(gridUid, mapGrid, coords));
            }

            // create a box around the cursor
            var gridSearchBox = Box2.UnitCentered.Scale(searchBoxSize).Translated(mapCoords.Position);

            // find grids in search box
            var gridsInArea = mapManager.FindGridsIntersecting(mapCoords.MapId, gridSearchBox);

            // find closest grid intersecting our search box.
            gridUid = EntityUid.Invalid;
            MapGridComponent? closest = null;
            var distance = float.PositiveInfinity;
            var intersect = new Box2();
            var xformQuery = entityManager.GetEntityQuery<TransformComponent>();

            foreach (var grid in gridsInArea)
            {
                var gridXform = xformQuery.GetComponent(grid.Owner);
                // TODO: Use CollisionManager to get nearest edge.

                // figure out closest intersect
                var gridIntersect = gridSearchBox.Intersect(gridXform.WorldMatrix.TransformBox(grid.LocalAABB));
                var gridDist = (gridIntersect.Center - mapCoords.Position).LengthSquared();

                if (gridDist >= distance)
                    continue;

                gridUid = grid.Owner;
                distance = gridDist;
                closest = grid;
                intersect = gridIntersect;
            }

            if (closest != null) // stick to existing grid
            {
                // round to nearest cardinal dir
                var normal = mapCoords.Position - intersect.Center;

                // round coords to center of tile
                var tileIndices = mapSystem.WorldToTile(gridUid, closest, intersect.Center);
                var tileCenterWorld = mapSystem.GridTileToWorldPos(gridUid, closest, tileIndices);

                // move mouse one tile out along normal
                var newTilePos = tileCenterWorld + normal * closest.TileSize;

                coords = new EntityCoordinates(gridUid, mapSystem.WorldToLocal(gridUid, closest, newTilePos));
            }
            //else free place

            return coords;
        }
    }
}
