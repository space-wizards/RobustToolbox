using Robust.Shared.Map;

namespace Robust.Client.Placement.Modes
{
    public sealed class PlaceNearby : PlacementMode
    {
        public PlaceNearby(PlacementManager pMan) : base(pMan) { }

        public override bool RangeRequired => true;

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToCursorGrid(mouseScreen);
            CurrentTile = GetTileRef(MouseCoords);
        }

        public override bool IsValidPosition(EntityCoordinates position)
        {
            if (pManager.CurrentPermission!.IsTile)
            {
                return false;
            }
            else if (!RangeCheck(position))
            {
                return false;
            }

            return true;
        }
    }
}
