using Robust.Shared.Map;

namespace Robust.Client.Placement.Modes
{
    public class PlaceFree : PlacementMode
    {
        public PlaceFree(PlacementManager pMan) : base(pMan) { }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToPlayerGrid(mouseScreen);
            CurrentTile = pManager.MapManager.GetGrid(MouseCoords.GridID).GetTile(MouseCoords);
        }

        public override bool IsValidPosition(GridCoordinates position)
        {
            return true;
        }
    }
}
