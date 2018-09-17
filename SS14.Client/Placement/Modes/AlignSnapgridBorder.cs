using System;
using SS14.Client.Graphics.ClientEye;
using SS14.Client.Utility;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.Placement.Modes
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

        public override void Render()
        {
            if (onGrid)
            {
                const int ppm = EyeManager.PIXELSPERMETER;
                var viewportSize = pManager.sceneTree.SceneTree.Root.Size.Convert();
                var position = pManager.eyeManager.ScreenToWorld(Vector2.Zero);
                var gridstartx = (float) Math.Round(position.X / snapSize, MidpointRounding.AwayFromZero) * snapSize;
                var gridstarty = (float) Math.Round(position.Y / snapSize, MidpointRounding.AwayFromZero) * snapSize;
                var gridstart = pManager.eyeManager.WorldToScreen(
                    new Vector2( //Find snap grid closest to screen origin and convert back to screen coords
                        gridstartx,
                        gridstarty));
                var flip = new Godot.Vector2(1, -1);
                for (var a = gridstart.X;
                    a < viewportSize.X;
                    a += snapSize * ppm) //Iterate through screen creating gridlines
                {
                    var from = ScreenToWorld(new Vector2(a, 0)).Convert() * ppm * flip;
                    var to = ScreenToWorld(new Vector2(a, viewportSize.Y)).Convert() * ppm * flip;
                    pManager.DrawNode.DrawLine(from, to, new Godot.Color(0, 0, 1), 0.5f);
                }

                for (var a = gridstart.Y; a < viewportSize.Y; a += snapSize * ppm)
                {
                    var from = ScreenToWorld(new Vector2(0, a)).Convert() * ppm * flip;
                    var to = ScreenToWorld(new Vector2(viewportSize.X, a)).Convert() * ppm * flip;
                    pManager.DrawNode.DrawLine(from, to, new Godot.Color(0, 0, 1), 0.5f);
                }
            }

            // Draw grid BELOW the ghost.
            base.Render();
        }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToPlayerGrid(mouseScreen);

            snapSize = MouseCoords.Grid.SnapSize; //Find snap size.
            GridDistancing = snapSize;
            onGrid = true;

            var mouselocal = new Vector2( //Round local coordinates onto the snap grid
                (float) Math.Round(MouseCoords.X / (double) snapSize, MidpointRounding.AwayFromZero) * snapSize,
                (float) Math.Round(MouseCoords.Y / (double) snapSize, MidpointRounding.AwayFromZero) * snapSize);

            //Convert back to original world and screen coordinates after applying offset
            MouseCoords =
                new GridLocalCoordinates(
                    mouselocal + new Vector2(pManager.PlacementOffset.X, pManager.PlacementOffset.Y), MouseCoords.Grid);
        }

        public override bool IsValidPosition(GridLocalCoordinates position)
        {
            if (!RangeCheck(position))
            {
                return false;
            }

            return true;
        }
    }
}
