using Robust.Shared.IoC;
using Robust.Client.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Placement.Modes
{
    public class AlignTileEmpty : PlacementMode
    {
        public override bool HasLineMode => true;
        public override bool HasGridMode => true;

        public AlignTileEmpty(PlacementManager pMan) : base(pMan) { }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToCursorGrid(mouseScreen);

            var mapGrid = pManager.MapManager.GetGrid(MouseCoords.GridID);
            CurrentTile = mapGrid.GetTileRef(MouseCoords);
            float tileSize = mapGrid.TileSize; //convert from ushort to float
            GridDistancing = tileSize;

            if (pManager.CurrentPermission!.IsTile)
            {
                MouseCoords = new GridCoordinates(CurrentTile.X + tileSize / 2,
                                                  CurrentTile.Y + tileSize / 2,
                                                  MouseCoords.GridID);
            }
            else
            {
                MouseCoords = new GridCoordinates(CurrentTile.X + tileSize / 2 + pManager.PlacementOffset.X,
                                                  CurrentTile.Y + tileSize / 2 + pManager.PlacementOffset.Y,
                                                  MouseCoords.GridID);
            }
        }

        public override bool IsValidPosition(GridCoordinates position)
        {
            if (!RangeCheck(position))
            {
                return false;
            }

            var entitymanager = IoCManager.Resolve<IClientEntityManager>();
            return !(entitymanager.AnyEntitiesIntersecting(pManager.MapManager.GetGrid(MouseCoords.GridID).ParentMapId,
                new Box2(new Vector2(CurrentTile.X, CurrentTile.Y), new Vector2(CurrentTile.X + 0.99f, CurrentTile.Y + 0.99f))));
        }
    }
}
