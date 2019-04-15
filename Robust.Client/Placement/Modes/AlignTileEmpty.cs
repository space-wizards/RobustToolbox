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
            MouseCoords = ScreenToPlayerGrid(mouseScreen);

            CurrentTile = MouseCoords.Grid.GetTile(MouseCoords);
            float tileSize = MouseCoords.Grid.TileSize; //convert from ushort to float
            GridDistancing = tileSize;

            if (pManager.CurrentPermission.IsTile)
            {
                MouseCoords = new GridCoordinates(CurrentTile.X + tileSize / 2,
                                                  CurrentTile.Y + tileSize / 2,
                                                  MouseCoords.Grid);
            }
            else
            {
                MouseCoords = new GridCoordinates(CurrentTile.X + tileSize / 2 + pManager.PlacementOffset.X,
                                                  CurrentTile.Y + tileSize / 2 + pManager.PlacementOffset.Y,
                                                  MouseCoords.Grid);
            }
        }

        public override bool IsValidPosition(GridCoordinates position)
        {
            if (!RangeCheck(position))
            {
                return false;
            }

            var entitymanager = IoCManager.Resolve<IClientEntityManager>();
            return !(entitymanager.AnyEntitiesIntersecting(MouseCoords.MapID,
                new Box2(new Vector2(CurrentTile.X, CurrentTile.Y), new Vector2(CurrentTile.X + 0.99f, CurrentTile.Y + 0.99f))));
        }
    }
}
