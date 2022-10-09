using Robust.Shared.Map;

namespace Robust.Client.Placement.Modes
{
    public sealed class AlignTileNonDense : PlacementMode
    {
        public override bool HasLineMode => true;
        public override bool HasGridMode => true;

        public AlignTileNonDense(PlacementManager pMan) : base(pMan) { }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToCursorGrid(mouseScreen);

            var tileSize = 1f;

            var gridId = MouseCoords.GetGridId(pManager.EntityManager);
            if (gridId.IsValid())
            {
                var mapGrid = pManager.MapManager.GetGrid(gridId);
                tileSize = mapGrid.TileSize; //convert from ushort to float
            }

            CurrentTile = GetTileRef(MouseCoords);
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
            return pManager.CurrentPermission!.IsTile || !IsColliding(position);
        }
    }
}
