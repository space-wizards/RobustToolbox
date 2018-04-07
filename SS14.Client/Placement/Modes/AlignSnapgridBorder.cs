using System;
using SS14.Client.Graphics;
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

        public SnapgridBorder(PlacementManager pMan) : base(pMan) { }

        public override void Render()
        {
            if (onGrid)
            {
                const int ppm = EyeManager.PIXELSPERMETER;
                var viewportSize = pManager.sceneTree.SceneTree.Root.Size.Convert();
                var position = pManager.eyeManager.ScreenToWorld(Vector2.Zero);
                var gridstartx = (float) Math.Round(position.X / snapSize, MidpointRounding.AwayFromZero) * snapSize;
                var gridstarty = (float) Math.Round(position.Y / snapSize, MidpointRounding.AwayFromZero) * snapSize;
                var gridstart = pManager.eyeManager.WorldToScreen(new Vector2( //Find snap grid closest to screen origin and convert back to screen coords
                    gridstartx,
                    gridstarty));
                for (var a = gridstart.X; a < viewportSize.X; a += snapSize * ppm) //Iterate through screen creating gridlines
                {
                    var from = ScreenToWorld(new Vector2(a, 0)).Convert() * ppm;
                    var to = ScreenToWorld(new Vector2(a, viewportSize.Y)).Convert() * ppm;
                    pManager.drawNode.DrawLine(from, to, new Godot.Color(0, 0, 1), 0.5f);
                }
                for (var a = gridstart.Y; a < viewportSize.Y; a += snapSize * ppm)
                {
                    var from = ScreenToWorld(new Vector2(0, a)).Convert() * ppm;
                    var to = ScreenToWorld(new Vector2(viewportSize.X, a)).Convert() * ppm;
                    pManager.drawNode.DrawLine(from, to, new Godot.Color(0, 0, 1), 0.5f);
                }
            }

            // Draw grid BELOW the ghost.
            base.Render();
        }

        public override bool FrameUpdate(RenderFrameEventArgs e, ScreenCoordinates mouseS)
        {
            if (mouseS.MapID == MapId.Nullspace)
            {
                onGrid = false;
                return false;
            }

            MouseScreen = mouseS;
            MouseCoords = pManager.eyeManager.ScreenToWorld(MouseScreen);

            snapSize = MouseCoords.Grid.SnapSize; //Find snap size.
            onGrid = true;

            var mouselocal = new Vector2( //Round local coordinates onto the snap grid
                (float) Math.Round(MouseCoords.X / (double) snapSize, MidpointRounding.AwayFromZero) * snapSize,
                (float) Math.Round(MouseCoords.Y / (double) snapSize, MidpointRounding.AwayFromZero) * snapSize);

            //Convert back to original world and screen coordinates after applying offset
            MouseCoords = new LocalCoordinates(mouselocal + new Vector2(pManager.CurrentPrototype.PlacementOffset.X, pManager.CurrentPrototype.PlacementOffset.Y), MouseCoords.Grid);
            MouseScreen = pManager.eyeManager.WorldToScreen(MouseCoords);

            if (!RangeCheck())
                return false;

            return true;
        }
    }
}
