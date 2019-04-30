using Robust.Shared.Map;

namespace Robust.Client.Placement.Modes
{
    public class AlignTileNonDense : PlacementMode
    {
        public override bool HasLineMode => true;
        public override bool HasGridMode => true;

        public AlignTileNonDense(PlacementManager pMan) : base(pMan) { }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToCursorGrid(mouseScreen);

            var mapGrid = pManager.MapManager.GetGrid(MouseCoords.GridID);
            CurrentTile = mapGrid.GetTileRef(MouseCoords);
            float tileSize = mapGrid.TileSize; //convert from ushort to float
            GridDistancing = tileSize;

            if (pManager.CurrentPermission.IsTile)
            {
                MouseCoords = new GridCoordinates(CurrentTile.X + tileSize / 2,
                                                 CurrentTile.Y + tileSize / 2,
                                                 MouseCoords.GridID);
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
            if (!pManager.CurrentPermission.IsTile && IsColliding(position))
            {
                return false;
            }

            return true;
        }
    }
}
