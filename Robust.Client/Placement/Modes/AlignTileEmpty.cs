using Robust.Shared.GameObjects;
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

            var tileSize = 1f;
            var gridId = MouseCoords.GetGridId(pManager.EntityManager);

            if (gridId.IsValid())
            {
                var mapGrid = pManager.MapManager.GetGrid(MouseCoords.GetGridId(pManager.EntityManager));
                CurrentTile = mapGrid.GetTileRef(MouseCoords);
                tileSize = mapGrid.TileSize; //convert from ushort to float
            }

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

            var map = MouseCoords.GetMapId(pManager.EntityManager);
            var bottomLeft = new Vector2(CurrentTile.X, CurrentTile.Y);
            var topRight = new Vector2(CurrentTile.X + 0.99f, CurrentTile.Y + 0.99f);
            var box = new Box2(bottomLeft, topRight);

            return !EntitySystem.Get<SharedEntityLookupSystem>().AnyEntitiesIntersecting(map, box);
        }
    }
}
