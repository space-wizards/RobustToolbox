using SS14.Client.Graphics;
using SS14.Shared.Map;

namespace SS14.Client.Placement.Modes
{
    public class AlignTileNonSolid : PlacementMode
    {
        public AlignTileNonSolid(PlacementManager pMan) : base(pMan)
        {
        }

        public override bool FrameUpdate(RenderFrameEventArgs e, ScreenCoordinates mouseS)
        {
            if (mouseS.MapID == MapId.Nullspace) return false;

            MouseScreen = mouseS;
            MouseCoords = pManager.eyeManager.ScreenToWorld(MouseScreen);

            CurrentTile = MouseCoords.Grid.GetTile(MouseCoords);
            var tilesize = MouseCoords.Grid.TileSize;

            if (!RangeCheck())
                return false;

            if (pManager.CurrentPermission.IsTile)
            {
                MouseCoords = new LocalCoordinates(CurrentTile.X + tilesize/2,
                                                 CurrentTile.Y + tilesize/2,
                                                 MouseCoords.Grid);
            }
            else
            {
                MouseCoords = new LocalCoordinates(CurrentTile.X + tilesize/2 + pManager.CurrentPrototype.PlacementOffset.X,
                                                  CurrentTile.Y + tilesize/2 + pManager.CurrentPrototype.PlacementOffset.Y,
                                                  MouseCoords.Grid);

//                if (CheckCollision())
//                    return false;
            }

            MouseScreen = pManager.eyeManager.WorldToScreen(MouseCoords);

            return true;
        }
    }
}
