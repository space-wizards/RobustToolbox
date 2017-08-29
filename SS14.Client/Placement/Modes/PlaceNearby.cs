using OpenTK;
using SFML.Graphics;
using SFML.System;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using Vector2i = SS14.Shared.Maths.Vector2i;

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
            if (currentMap == null) return false;

            mouseScreen = mouseS;
            mouseWorld = CluwneLib.ScreenToWorld(mouseScreen);
            currentTile = currentMap.GetDefaultGrid().GetTile(mouseWorld);

            if (pManager.CurrentPermission.IsTile)
                return false;

            if (!RangeCheck())
                return false;

            return true;
        }
    }
}
