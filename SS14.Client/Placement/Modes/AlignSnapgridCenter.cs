using System;
using SS14.Client.Graphics;
using SS14.Client.Utility;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.Placement.Modes
{
    public class SnapgridCenter : PlacementMode
    {
        bool onGrid;
        float snapSize;

        public SnapgridCenter(PlacementManager pMan) : base(pMan)
        {
        }

        public override void Render()
        {
            if (onGrid)
            {
                var viewportSize = pManager.sceneTree.SceneTree.Root.Size.Convert();
                var position = pManager.eyeManager.ScreenToWorld(Vector2.Zero);
                var gridstart = pManager.eyeManager.WorldToScreen(new Vector2( //Find snap grid closest to screen origin and convert back to screen coords
                    (float)(Math.Round(position.X/snapSize - 0.5f, MidpointRounding.AwayFromZero)+0.5f) * snapSize,
                    (float)(Math.Round(position.Y/snapSize - 0.5f, MidpointRounding.AwayFromZero)+0.5f) * snapSize));
                for (var a = gridstart.X; a < viewportSize.X; a += snapSize * 32) //Iterate through screen creating gridlines
                {
                    var from = ScreenToWorld(new Vector2(a, 0)).Convert();
                    var to = ScreenToWorld(new Vector2(a, viewportSize.Y)).Convert();
                    pManager.drawNode.DrawLine(from, to, new Godot.Color(0, 0, 1), 0.5f);
                }
                for (var a = gridstart.Y; a < viewportSize.Y; a += snapSize * 32)
                {
                    var from = ScreenToWorld(new Vector2(0, a)).Convert();
                    var to = ScreenToWorld(new Vector2(viewportSize.X, a)).Convert();
                    pManager.drawNode.DrawLine(from, to, new Godot.Color(0, 0, 1), 0.5f);
                }
            }

            // Draw grid BELOW the ghost thing.
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

            var mouseLocal = new Vector2( //Round local coordinates onto the snap grid
                (float)(Math.Round((MouseCoords.Position.X/(double)snapSize - 0.5f), MidpointRounding.AwayFromZero)+0.5) * snapSize,
                (float)(Math.Round((MouseCoords.Position.Y/(double)snapSize - 0.5f), MidpointRounding.AwayFromZero)+0.5) * snapSize);

            //Adjust mouseCoords to new calculated position
            MouseCoords = new LocalCoordinates(mouseLocal + new Vector2(pManager.CurrentPrototype.PlacementOffset.X, pManager.CurrentPrototype.PlacementOffset.Y), MouseCoords.Grid);
            MouseScreen = pManager.eyeManager.WorldToScreen(MouseCoords);

            if (!RangeCheck())
                return false;

            return true;
        }
    }
}
