using System;
using SS14.Client.Graphics;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.Placement.Modes
{
    public class SnapgridBorder : PlacementMode
    {
        private bool _onGrid;
        private float _snapSize;

        public SnapgridBorder(PlacementManager pMan) : base(pMan) { }

        public override void Render()
        {
            /*
            base.Render();
            if (_onGrid)
            {
                var position = CluwneLib.ScreenToCoordinates(new ScreenCoordinates(0, 0, MouseCoords.MapID)); //Find world coordinates closest to screen origin
                var gridstart = CluwneLib.WorldToScreen(new Vector2( //Find snap grid closest to screen origin and convert back to screen coords
                    (float) Math.Round(position.X / _snapSize, MidpointRounding.AwayFromZero) * _snapSize,
                    (float) Math.Round(position.Y / _snapSize, MidpointRounding.AwayFromZero) * _snapSize));
                for (var a = gridstart.X; a < CluwneLib.Window.Viewport.Size.X; a += _snapSize * 32) //Iterate through screen creating gridlines
                {
                    CluwneLib.drawLine(a, 0, CluwneLib.Window.Viewport.Size.Y, 90, 0.5f, Color.Blue);
                }
                for (var a = gridstart.Y; a < CluwneLib.Window.Viewport.Size.Y; a += _snapSize * 32)
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

            var snapsize = MouseCoords.Grid.SnapSize; //Find snap size.

            var mouselocal = new Vector2( //Round local coordinates onto the snap grid
                (float) Math.Round(MouseCoords.X / (double) snapsize, MidpointRounding.AwayFromZero) * snapsize,
                (float) Math.Round(MouseCoords.Y / (double) snapsize, MidpointRounding.AwayFromZero) * snapsize);

            //Convert back to original world and screen coordinates after applying offset
            MouseCoords = new LocalCoordinates(mouselocal + new Vector2(pManager.CurrentPrototype.PlacementOffset.X, pManager.CurrentPrototype.PlacementOffset.Y), MouseCoords.Grid);
            MouseScreen = pManager.eyeManager.WorldToScreen(MouseCoords);

            if (!RangeCheck())
                return false;

            return true;
        }
    }
}
