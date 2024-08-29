﻿using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Robust.Client.Placement.Modes
{
    public sealed class AlignTileAny : PlacementMode
    {
        public override bool HasLineMode => true;
        public override bool HasGridMode => true;

        public AlignTileAny(PlacementManager pMan) : base(pMan) { }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            // Go over diagonal size so when placing in a line it doesn't stop snapping.
            const float SearchBoxSize = 2f; // size of search box in meters

            MouseCoords = ScreenToCursorGrid(mouseScreen).AlignWithClosestGridTile(SearchBoxSize, pManager.EntityManager, pManager.MapManager);

            var gridId = MouseCoords.GetGridUid(pManager.EntityManager);

            if (!pManager.EntityManager.TryGetComponent<MapGridComponent>(gridId, out var mapGrid))
                return;

            CurrentTile = mapGrid.GetTileRef(MouseCoords);
            float tileSize = mapGrid.TileSize; //convert from ushort to float
            GridDistancing = tileSize;

            if (pManager.CurrentPermission!.IsTile)
            {
                MouseCoords = new EntityCoordinates(MouseCoords.EntityId, new Vector2(CurrentTile.X + tileSize / 2,
                    CurrentTile.Y + tileSize / 2));
            }
            else
            {
                MouseCoords = new EntityCoordinates(MouseCoords.EntityId, new Vector2(CurrentTile.X + tileSize / 2 + pManager.PlacementOffset.X,
                    CurrentTile.Y + tileSize / 2 + pManager.PlacementOffset.Y));
            }
        }

        public override bool IsValidPosition(EntityCoordinates position)
        {
            if (!RangeCheck(position))
            {
                return false;
            }

            return true;
        }
    }
}
