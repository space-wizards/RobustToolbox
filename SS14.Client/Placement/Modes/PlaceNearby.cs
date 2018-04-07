using SS14.Client.Graphics;
using SS14.Shared.Map;

namespace SS14.Client.Placement.Modes
{
    public class PlaceNearby : PlacementMode
    {
        public PlaceNearby(PlacementManager pMan) : base(pMan)
        {
        }

        public override bool rangerequired => true;

        public override bool FrameUpdate(RenderFrameEventArgs e, ScreenCoordinates mouseS)
        {
            if (mouseS.MapID == MapId.Nullspace) return false;

            MouseScreen = mouseS;
            MouseCoords = pManager.eyeManager.ScreenToWorld(MouseScreen);
            CurrentTile = MouseCoords.Grid.GetTile(MouseCoords);

            if (pManager.CurrentPermission.IsTile)
                return false;

            if (!RangeCheck())
                return false;

            return true;
        }
    }
}
