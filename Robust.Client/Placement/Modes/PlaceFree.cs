using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Client.Placement.Modes
{
    public class PlaceFree : PlacementMode
    {
        public PlaceFree(PlacementManager pMan) : base(pMan) { }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToCursorGrid(mouseScreen);
            var gridId = MouseCoords.GetGridId(pManager.EntityManager);
            CurrentTile = pManager.MapManager.GetGrid(gridId).GetTileRef(MouseCoords);
        }

        public override bool IsValidPosition(EntityCoordinates position)
        {
            return true;
        }
    }
}
