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

namespace SS14.Client.Placement.Modes
{
    public class PlaceNearby : PlacementMode
    {
        public PlaceNearby(PlacementManager pMan) : base(pMan)
        {
        }

        public override bool rangerequired => true;

        public override bool Update(ScreenCoordinates mouseS)
        {
            if (mouseS.MapID == MapManager.NULLSPACE) return false;

            mouseScreen = mouseS;
            mouseCoords = CluwneLib.ScreenToCoordinates(mouseScreen);
            currentTile = mouseCoords.Grid.GetTile(mouseCoords);

            if (pManager.CurrentPermission.IsTile)
                return false;

            if (!RangeCheck())
                return false;

            return true;
        }
    }
}
