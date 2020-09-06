using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Client.Placement.Modes
{
    public class AlignTileDense : PlacementMode
    {
        public override bool HasLineMode => true;
        public override bool HasGridMode => true;

        public AlignTileDense(PlacementManager pMan) : base(pMan) { }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToCursorGrid(mouseScreen);

            var mapGrid = pManager.MapManager.GetGrid(MouseCoords.GetGridId(pManager.EntityManager));
            CurrentTile = mapGrid.GetTileRef(MouseCoords);
            float tileSize = mapGrid.TileSize; //convert from ushort to float
            GridDistancing = tileSize;

            if (pManager.CurrentPermission!.IsTile)
            {
                MouseCoords = new EntityCoordinates(MouseCoords.EntityId,
                    (CurrentTile.X + tileSize / 2, CurrentTile.Y + tileSize / 2));
            }
            else
            {
                MouseCoords = new EntityCoordinates(MouseCoords.EntityId,
                    (CurrentTile.X + tileSize / 2 + pManager.PlacementOffset.X,
                        CurrentTile.Y + tileSize / 2 + pManager.PlacementOffset.Y));
            }
        }

        public override bool IsValidPosition(EntityCoordinates position)
        {
            if (!RangeCheck(position))
            {
                return false;
            }
            if (!pManager.CurrentPermission!.IsTile && !IsColliding(position))
            {
                return false;
            }

            return true;
        }
    }
}
