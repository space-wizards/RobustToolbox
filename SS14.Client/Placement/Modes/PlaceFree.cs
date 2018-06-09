using SS14.Shared.Map;

namespace SS14.Client.Placement.Modes
{
    public class PlaceFree : PlacementMode
    {
        public PlaceFree(PlacementManager pMan) : base(pMan) { }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToPlayerGrid(mouseScreen);
            CurrentTile = MouseCoords.Grid.GetTile(MouseCoords);
        }

        public override bool IsValidPosition(GridLocalCoordinates position)
        {
            return true;
        }
    }
}
