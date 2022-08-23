using Robust.Shared.Map;

namespace Robust.Client.Placement.Modes
{
    public sealed class PlaceFree : PlacementMode
    {
        public PlaceFree(PlacementManager pMan) : base(pMan) { }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToCursorGrid(mouseScreen);
            CurrentTile = GetTileRef(MouseCoords);
        }

        public override bool IsValidPosition(EntityCoordinates position)
        {
            return true;
        }
    }
}
