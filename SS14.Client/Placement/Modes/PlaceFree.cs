using SS14.Client.Graphics;
using SS14.Shared.Map;

namespace SS14.Client.Placement.Modes
{
    public class PlaceFree : PlacementMode
    {
        public PlaceFree(PlacementManager pMan) : base(pMan)
        {
        }

        public override bool Update(ScreenCoordinates mouseS)
        {
            if (mouseS.MapID == MapId.Nullspace) return false;

            MouseScreen = mouseS;
            MouseCoords = CluwneLib.ScreenToCoordinates(MouseScreen);
            CurrentTile = MouseCoords.Grid.GetTile(MouseCoords);

            return true;
        }
    }
}
