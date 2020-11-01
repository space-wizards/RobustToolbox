using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Placement.Modes
{
    public class AlignTileAny : PlacementMode
    {
        public override bool HasLineMode => true;
        public override bool HasGridMode => true;

        public AlignTileAny(PlacementManager pMan) : base(pMan) { }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            const float SearchBoxSize = 1.5f; // size of search box in meters

            MouseCoords = ScreenToCursorGrid(mouseScreen);

            var gridId = MouseCoords.GetGridId(pManager.EntityManager);

            IMapGrid? mapGrid = null;

            if (!gridId.IsValid() || !pManager.MapManager.TryGetGrid(gridId, out mapGrid))
            {
                // create a box around the cursor
                var gridSearchBox = Box2.UnitCentered.Scale(SearchBoxSize).Translated(MouseCoords.Position);

                // find grids in search box
                var gridsInArea = pManager.MapManager.FindGridsIntersecting(MouseCoords.GetMapId(pManager.EntityManager), gridSearchBox);

                // find closest grid intersecting our search box.
                IMapGrid? closest = null;
                var distance = float.PositiveInfinity;
                var intersect = new Box2();
                foreach (var grid in gridsInArea)
                {
                    // figure out closest intersect
                    var gridIntersect = gridSearchBox.Intersect(grid.WorldBounds);
                    var gridDist = (gridIntersect.Center - MouseCoords.Position).LengthSquared;

                    if (gridDist >= distance)
                        continue;

                    distance = gridDist;
                    closest = grid;
                    intersect = gridIntersect;
                }

                if (closest != null) // stick to existing grid
                {
                    // round to nearest cardinal dir
                    var normal = new Angle(MouseCoords.Position - intersect.Center).GetCardinalDir().ToVec();

                    // round coords to center of tile
                    var tileIndices = closest.WorldToTile(intersect.Center);
                    var tileCenterWorld = closest.GridTileToWorldPos(tileIndices);

                    // move mouse one tile out along normal
                    var newTilePos = tileCenterWorld + normal * closest.TileSize;

                    MouseCoords = new EntityCoordinates(closest.GridEntityId, closest.WorldToLocal(newTilePos));
                    mapGrid = closest;
                }
                //else free place
            }

            if (mapGrid == null)
                return;

            CurrentTile = mapGrid.GetTileRef(MouseCoords);
            float tileSize = mapGrid.TileSize; //convert from ushort to float
            GridDistancing = tileSize;

            if (pManager.CurrentPermission!.IsTile)
            {
                MouseCoords = new EntityCoordinates(MouseCoords.EntityId, (CurrentTile.X + tileSize / 2,
                    CurrentTile.Y + tileSize / 2));
            }
            else
            {
                MouseCoords = new EntityCoordinates(MouseCoords.EntityId, (CurrentTile.X + tileSize / 2 + pManager.PlacementOffset.X,
                    CurrentTile.Y + tileSize / 2 + pManager.PlacementOffset.Y));
            }
        }

        public override bool IsValidPosition(EntityCoordinates position)
        {
            if (!RangeCheck(position))
            {
                return false;
            }

            return true;
        }
    }
}
