using System.CodeDom;
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

            var mapGrid = pManager.MapManager.GetGrid(MouseCoords.GridID);

            if (mapGrid.IsDefaultGrid)
            {
                // check if we are on an edge of a grid
                // create a box around the cursor
                DebugTools.Assert(mapGrid.WorldPosition == Vector2.Zero); // assert that LocalPos == WorldPos
                var gridSearchBox = Box2.UnitCentered.Scale(SearchBoxSize).Translated(MouseCoords.Position);

                // find grids in search box
                var gridsInArea = pManager.MapManager.FindGridsIntersecting(mapGrid.ParentMapId, gridSearchBox);

                // find closest grid intersecting our search box.
                IMapGrid closest = null;
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
                    MouseCoords = new GridCoordinates(closest.WorldToLocal(newTilePos), closest.Index); 
                }
                //else free place
            }


            CurrentTile = mapGrid.GetTileRef(MouseCoords);
            float tileSize = mapGrid.TileSize; //convert from ushort to float
            GridDistancing = tileSize;

            if (pManager.CurrentPermission.IsTile)
            {
                if(!mapGrid.IsDefaultGrid)
                {
                    MouseCoords = new GridCoordinates(CurrentTile.X + tileSize / 2,
                        CurrentTile.Y + tileSize / 2,
                        MouseCoords.GridID);
                }
                // else we don't modify coords
            }
            else
            {
                MouseCoords = new GridCoordinates(CurrentTile.X + tileSize / 2 + pManager.PlacementOffset.X,
                    CurrentTile.Y + tileSize / 2 + pManager.PlacementOffset.Y,
                    MouseCoords.GridID);
            }
        }

        public override bool IsValidPosition(GridCoordinates position)
        {
            if (!RangeCheck(position))
            {
                return false;
            }

            return true;
        }
    }
}
