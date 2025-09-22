using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Robust.Client.Placement.Modes
{
    public sealed class AlignTileDense : PlacementMode
    {
        public override bool HasLineMode => true;
        public override bool HasGridMode => true;

        public AlignTileDense(PlacementManager pMan) : base(pMan) { }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToCursorGrid(mouseScreen);

            var transformSys = pManager.EntityManager.System<SharedTransformSystem>();

            var tileSize = 1f;
            var gridIdOpt = transformSys.GetGrid(MouseCoords);

            if (gridIdOpt is EntityUid gridId && gridId.IsValid())
            {
                var mapGrid = pManager.EntityManager.GetComponent<MapGridComponent>(gridId);
                tileSize = mapGrid.TileSize; //convert from ushort to float
            }

            CurrentTile = GetTileRef(MouseCoords);
            GridDistancing = tileSize;

            if (pManager.CurrentPermission!.IsTile)
            {
                MouseCoords = new EntityCoordinates(MouseCoords.EntityId,
                    new Vector2(CurrentTile.X + tileSize / 2, CurrentTile.Y + tileSize / 2));
            }
            else
            {
                MouseCoords = new EntityCoordinates(MouseCoords.EntityId,
                    new Vector2(CurrentTile.X + tileSize / 2 + pManager.PlacementOffset.X,
                        CurrentTile.Y + tileSize / 2 + pManager.PlacementOffset.Y));
            }
        }

        public override bool IsValidPosition(EntityCoordinates position)
        {
            if (!RangeCheck(position))
            {
                return false;
            }

            return pManager.CurrentPermission!.IsTile || IsColliding(position);
        }
    }
}
