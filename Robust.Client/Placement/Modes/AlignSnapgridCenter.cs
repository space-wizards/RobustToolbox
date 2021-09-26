using System;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Placement.Modes
{
    public class SnapgridCenter : PlacementMode
    {
        protected IMapGrid? Grid;
        protected float SnapSize;

        public override bool HasLineMode => true;
        public override bool HasGridMode => true;

        public SnapgridCenter(PlacementManager pMan) : base(pMan) { }

        public override void Render(DrawingHandleWorld handle)
        {
            if (Grid != null)
            {
                var viewportSize = (Vector2)pManager._clyde.ScreenSize;

                var gridPosition = Grid.MapToGrid(pManager.eyeManager.ScreenToMap(Vector2.Zero));

                var gridstart = pManager.eyeManager.CoordinatesToScreen(
                    gridPosition.WithPosition(new Vector2(MathF.Floor(gridPosition.X), MathF.Floor(gridPosition.Y))));

                for (var a = gridstart.X; a < viewportSize.X; a += SnapSize * EyeManager.PixelsPerMeter) //Iterate through screen creating gridlines
                {
                    var from = ScreenToWorld(new Vector2(a, 0));
                    var to = ScreenToWorld(new Vector2(a, viewportSize.Y));
                    handle.DrawLine(from, to, new Color(0, 0, 1f));
                }
                for (var a = gridstart.Y; a < viewportSize.Y; a += SnapSize * EyeManager.PixelsPerMeter)
                {
                    var from = ScreenToWorld(new Vector2(0, a));
                    var to = ScreenToWorld(new Vector2(viewportSize.X, a));
                    handle.DrawLine(from, to, new Color(0, 0, 1f));
                }
            }

            // Draw grid BELOW the ghost thing.
            base.Render(handle);
        }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToCursorGrid(mouseScreen);

            var gridId = MouseCoords.GetGridId(pManager.EntityManager);
            SnapSize = 1f;
            if (gridId.IsValid())
            {
                Grid = pManager.MapManager.GetGrid(gridId);
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
