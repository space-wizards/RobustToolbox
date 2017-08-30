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
using SS14.Shared.Map;

namespace SS14.Client.Placement.Modes
{
    public class AlignTileAny : PlacementMode
    {
        public AlignTileAny(PlacementManager pMan) : base(pMan)
        {
        }

        public override bool Update(ScreenCoordinates mouseS)
        {
            if (mouseS.MapID == Coordinates.NULLSPACE) return false;

            mouseScreen = mouseS;
            mouseWorld = CluwneLib.ScreenToWorld(mouseScreen);

            currentTile = currentMap.GetDefaultGrid().GetTile(mouseWorld);

            if (!RangeCheck())
                return false;

            if (pManager.CurrentPermission.IsTile)
            {
                mouseWorld = new WorldCoordinates(currentTile.X + 0.5f,
                                                  currentTile.Y + 0.5f,
                                                  mouseS.MapID);
                mouseScreen = CluwneLib.WorldToScreen(mouseWorld);
            }
            else
            {
                mouseWorld = new WorldCoordinates(currentTile.X + 0.5f + pManager.CurrentPrototype.PlacementOffset.X,
                                                  currentTile.Y + 0.5f + pManager.CurrentPrototype.PlacementOffset.Y,
                                                  mouseS.MapID);
                mouseScreen = CluwneLib.WorldToScreen(mouseWorld);
            }

            return true;
        }
    }
}
