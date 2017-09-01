using OpenTK;
using SFML.Graphics;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Utility;
using SS14.Shared.Maths;
using System;
using OpenTK.Graphics;
using SS14.Shared.IoC;
using SS14.Shared.Prototypes;

namespace SS14.Client.Placement.Modes
{
    public class SnapgridCenter : PlacementMode
    {
        bool ongrid;
        float snapsize;

        public SnapgridCenter(PlacementManager pMan) : base(pMan)
        {
        }

        public override void Render()
        {
            base.Render();
            if (ongrid)
            {
                var position = CluwneLib.ScreenToWorld(new Vector2i(0, 0)); //Find world coordinates closest to screen origin
                var gridstart = CluwneLib.WorldToScreen(new Vector2( //Find snap grid closest to screen origin and convert back to screen coords
                (float)(Math.Round(((position.X / snapsize)-0.5), MidpointRounding.AwayFromZero)+0.5) * snapsize,
                (float)(Math.Round(((position.Y / snapsize)-0.5), MidpointRounding.AwayFromZero)+0.5) * snapsize));
                for (float a = gridstart.X; a < CluwneLib.Window.Viewport.Size.X; a += snapsize * 32) //Iterate through screen creating gridlines
                {
                    CluwneLib.drawLine(a, 0, CluwneLib.Window.Viewport.Size.Y, 90, 0.5f, Color4.Blue);
                }
                for (float a = gridstart.Y; a < CluwneLib.Window.Viewport.Size.Y; a += snapsize * 32)
                {
                    CluwneLib.drawLine(0, a, CluwneLib.Window.Viewport.Size.X, 0, 0.5f, Color4.Blue);
                }
            }
        }

        public override bool Update(Vector2i mouseS, IMapManager currentMap)
        {
            if (currentMap == null) return false;

            mouseScreen = mouseS;
            mouseWorld = CluwneLib.ScreenToWorld(mouseScreen);

            if (! (ongrid = currentMap.TryFindGridAt(mouseWorld, out IMapGrid currentgrid))) //Cant find a grid
                return false;

            var mouselocal = currentgrid.WorldToLocal(mouseWorld); //Convert code to local grid coordinates
            snapsize = currentgrid.SnapSize; //Find snap size.

            mouselocal = new Vector2( //Round local coordinates onto the snap grid
                (float)(Math.Round(((mouselocal.X / snapsize)- 0.5), MidpointRounding.AwayFromZero)+ 0.5) * snapsize,
                (float)(Math.Round(((mouselocal.Y / snapsize)- 0.5), MidpointRounding.AwayFromZero)+ 0.5) * snapsize);

            //Convert back to original world and screen coordinates after applying offset
            mouseWorld = currentgrid.LocalToWorld(mouselocal) + new Vector2(pManager.CurrentPrototype.PlacementOffset.X, pManager.CurrentPrototype.PlacementOffset.Y);
            mouseScreen = (Vector2i)CluwneLib.WorldToScreen(mouseWorld);

            if (!pManager.CurrentPrototype.CanSpawnAt(currentgrid, mouselocal))
                return false;

            if (!RangeCheck())
                return false;

            return true;
        }
    }
}
