using System;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Graphics.Drawing;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Placement.Modes
{
    public class SnapgridBorder : PlacementMode
    {
        private bool onGrid;
        private float snapSize;

        public override bool HasLineMode => true;
        public override bool HasGridMode => true;

        public SnapgridBorder(PlacementManager pMan) : base(pMan)
        {
        }

        public override void Render(DrawingHandleWorld handle)
        {
            if (onGrid)
            {
                const int ppm = EyeManager.PixelsPerMeter;
                var viewportSize = (Vector2)pManager._clyde.ScreenSize;

                var position = pManager.eyeManager.ScreenToMap(Vector2.Zero);

                var gridStartX = (float) MathF.Round(position.X / snapSize, MidpointRounding.AwayFromZero) * snapSize;
                var gridStartY = (float) MathF.Round(position.Y / snapSize, MidpointRounding.AwayFromZero) * snapSize;
                var gridStart = pManager.eyeManager.WorldToScreen(
                    new Vector2( //Find snap grid closest to screen origin and convert back to screen coords
                        gridStartX,
                        gridStartY));
                for (var a = gridStart.X;
                    a < viewportSize.X;
                    a += snapSize * ppm) //Iterate through screen creating gridlines
                {
                    var from = ScreenToWorld(new Vector2(a, 0));
                    var to = ScreenToWorld(new Vector2(a, viewportSize.Y));
                    handle.DrawLine(from, to, new Color(0, 0, 1f));
                }

                for (var a = gridStart.Y; a < viewportSize.Y; a += snapSize * ppm)
                {
                    var from = ScreenToWorld(new Vector2(0, a));
                    var to = ScreenToWorld(new Vector2(viewportSize.X, a));
                    handle.DrawLine(from, to, new Color(0, 0, 1f));
                }
            }

            // Draw grid BELOW the ghost.
            base.Render(handle);
        }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToCursorGrid(mouseScreen);

            snapSize = pManager.MapManager.GetGrid(MouseCoords.GetGridId(pManager.EntityManager)).SnapSize; //Find snap size.
            GridDistancing = snapSize;
            onGrid = true;

            var mouseLocal = new Vector2( //Round local coordinates onto the snap grid
                (float) MathF.Round(MouseCoords.X / snapSize, MidpointRounding.AwayFromZero) * snapSize,
                (float) MathF.Round(MouseCoords.Y / snapSize, MidpointRounding.AwayFromZero) * snapSize);

            //Convert back to original world and screen coordinates after applying offset
            MouseCoords =
                new EntityCoordinates(
                    MouseCoords.EntityId, mouseLocal + new Vector2(pManager.PlacementOffset.X, pManager.PlacementOffset.Y));
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
