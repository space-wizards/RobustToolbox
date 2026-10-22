using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Robust.Client.Placement.Modes
{
    [Virtual]
    public class SnapgridCenter : PlacementMode
    {
        protected MapGridComponent? Grid;
        protected float SnapSize;

        public override bool HasLineMode => true;
        public override bool HasGridMode => true;

        public SnapgridCenter(PlacementManager pMan) : base(pMan) { }

        public override void Render(in OverlayDrawArgs args)
        {
            if (Grid != null)
            {
                var viewportSize = (Vector2)pManager.Clyde.ScreenSize;

                var gridUid = pManager.EntityManager.System<SharedTransformSystem>().GetGrid(MouseCoords);

                var gridPosition = pManager.EntityManager.System<SharedMapSystem>().MapToGrid(gridUid!.Value, pManager.EyeManager.ScreenToMap(Vector2.Zero));

                var gridstart = pManager.EyeManager.CoordinatesToScreen(
                    gridPosition.WithPosition(new Vector2(MathF.Floor(gridPosition.X), MathF.Floor(gridPosition.Y))));

                for (var a = gridstart.X; a < viewportSize.X; a += SnapSize * EyeManager.PixelsPerMeter) //Iterate through screen creating gridlines
                {
                    var from = ScreenToWorld(new Vector2(a, 0));
                    var to = ScreenToWorld(new Vector2(a, viewportSize.Y));
                    args.WorldHandle.DrawLine(from, to, new Color(0, 0, 0.3f));
                }
                for (var a = gridstart.Y; a < viewportSize.Y; a += SnapSize * EyeManager.PixelsPerMeter)
                {
                    var from = ScreenToWorld(new Vector2(0, a));
                    var to = ScreenToWorld(new Vector2(viewportSize.X, a));
                    args.WorldHandle.DrawLine(from, to, new Color(0, 0, 0.3f));
                }
            }

            // Draw grid BELOW the ghost thing.
            base.Render(args);
        }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToCursorGrid(mouseScreen);

            var gridIdOpt = pManager.EntityManager.System<SharedTransformSystem>().GetGrid(MouseCoords);
            SnapSize = 1f;
            if (gridIdOpt is { } gridId && gridId.IsValid())
            {
                Grid = pManager.EntityManager.GetComponent<MapGridComponent>(gridId);
                SnapSize = Grid.TileSize; //Find snap size for the grid.
            }
            else
            {
                Grid = null;
            }

            GridDistancing = SnapSize;

            var mouseLocal = new Vector2( //Round local coordinates onto the snap grid
                (float)(MathF.Round((MouseCoords.Position.X / SnapSize - 0.5f), MidpointRounding.AwayFromZero) + 0.5) * SnapSize,
                (float)(MathF.Round((MouseCoords.Position.Y / SnapSize - 0.5f), MidpointRounding.AwayFromZero) + 0.5) * SnapSize);

            //Adjust mouseCoords to new calculated position
            MouseCoords = new EntityCoordinates(MouseCoords.EntityId, mouseLocal + new Vector2(pManager.PlacementOffset.X, pManager.PlacementOffset.Y));
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
