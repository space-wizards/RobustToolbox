using SS14.Client.Graphics;
using SS14.Shared.Map;

namespace SS14.Client.Placement.Modes
{
    public class AlignTileAny : PlacementMode
    {
        public AlignTileAny(PlacementManager pMan) : base(pMan) { }

        public override bool Update(ScreenCoordinates mouseS)
        {
            if (mouseS.MapID == MapId.Nullspace) return false;

            MouseScreen = mouseS;
            MouseCoords = CluwneLib.ScreenToCoordinates(MouseScreen);

            CurrentTile = MouseCoords.Grid.GetTile(MouseCoords);
            var tileSize = MouseCoords.Grid.TileSize;

            if (!RangeCheck())
                return false;

            if (pManager.CurrentPermission.IsTile)
            {
                MouseCoords = new LocalCoordinates(CurrentTile.X + tileSize / 2,
                    CurrentTile.Y + tileSize / 2,
                    MouseCoords.Grid);
                MouseScreen = CluwneLib.WorldToScreen(MouseCoords);
            }
            else
            {
                MouseCoords = new LocalCoordinates(CurrentTile.X + tileSize / 2 + pManager.CurrentPrototype.PlacementOffset.X,
                    CurrentTile.Y + tileSize / 2 + pManager.CurrentPrototype.PlacementOffset.Y,
                    MouseCoords.Grid);
                MouseScreen = CluwneLib.WorldToScreen(MouseCoords);
            }

            return true;
        }
    }
}
