using System;
using SS14.Client.Graphics;
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
            /*
            base.Render();
            if (onGrid)
            {
                var position = CluwneLib.ScreenToCoordinates(new ScreenCoordinates(0,0,MouseCoords.MapID)); //Find world coordinates closest to screen origin
                var gridstart = CluwneLib.WorldToScreen(new Vector2( //Find snap grid closest to screen origin and convert back to screen coords

                (float)(Math.Round(((position.X / snapSize)-0.5), MidpointRounding.AwayFromZero)+0.5) * snapSize,
                (float)(Math.Round(((position.Y / snapSize)-0.5), MidpointRounding.AwayFromZero)+0.5) * snapSize));
                for (float a = gridstart.X; a < CluwneLib.Window.Viewport.Size.X; a += snapSize * 32) //Iterate through screen creating gridlines
                {
                    CluwneLib.drawLine(a, 0, CluwneLib.Window.Viewport.Size.Y, 90, 0.5f, Color.Blue);
                }
                for (float a = gridstart.Y; a < CluwneLib.Window.Viewport.Size.Y; a += snapSize * 32)
                {
                    CluwneLib.drawLine(0, a, CluwneLib.Window.Viewport.Size.X, 0, 0.5f, Color.Blue);
                }
            }
            */
        }

        public override bool FrameUpdate(RenderFrameEventArgs e, ScreenCoordinates mouseS)
        {
            if (mouseS.MapID == MapId.Nullspace) return false;

            MouseScreen = mouseS;
            MouseCoords = pManager.eyeManager.ScreenToWorld(MouseScreen);

            var snapSize = MouseCoords.Grid.SnapSize; //Find snap size.

            var mouseLocal = new Vector2( //Round local coordinates onto the snap grid
                (float)(Math.Round((MouseCoords.Position.X / (double)snapSize-0.5), MidpointRounding.AwayFromZero)+0.5) * snapSize,
                (float)(Math.Round((MouseCoords.Position.Y / (double)snapSize-0.5), MidpointRounding.AwayFromZero)+0.5) * snapSize);

            //Adjust mouseCoords to new calculated position
            MouseCoords = new LocalCoordinates(mouseLocal + new Vector2(pManager.CurrentPrototype.PlacementOffset.X, pManager.CurrentPrototype.PlacementOffset.Y), MouseCoords.Grid);
            MouseScreen = pManager.eyeManager.WorldToScreen(MouseCoords);

            if (!RangeCheck())
                return false;

            return true;
        }
    }
}
