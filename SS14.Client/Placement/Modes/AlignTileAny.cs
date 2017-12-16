using OpenTK;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using Vector2i = SS14.Shared.Maths.Vector2i;
using SS14.Shared.Map;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.Placement.Modes
{
    public class AlignTileAny : PlacementMode
    {
        public AlignTileAny(PlacementManager pMan) : base(pMan)
        {
        }

        public override bool Update(ScreenCoordinates mouseS)
        {
            if (mouseS.MapID == MapManager.NULLSPACE) return false;

            MouseScreen = mouseS;
            MouseCoords = CluwneLib.ScreenToCoordinates(MouseScreen);

            CurrentTile = MouseCoords.Grid.GetTile(MouseCoords);
            var tilesize = MouseCoords.Grid.TileSize;

            if (!RangeCheck())
                return false;

            if (pManager.CurrentPermission.IsTile)
            {
                MouseCoords = new LocalCoordinates(CurrentTile.X + tilesize/2,
                                                  CurrentTile.Y + tilesize/2,
                                                  MouseCoords.Grid);
                MouseScreen = CluwneLib.WorldToScreen(MouseCoords);
            }
            else
            {
                MouseCoords = new LocalCoordinates(CurrentTile.X + tilesize/2 + pManager.CurrentPrototype.PlacementOffset.X,
                                                  CurrentTile.Y + tilesize/2 + pManager.CurrentPrototype.PlacementOffset.Y,
                                                  MouseCoords.Grid);
                MouseScreen = CluwneLib.WorldToScreen(MouseCoords);
            }

            return true;
        }
    }
}
