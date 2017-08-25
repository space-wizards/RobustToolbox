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

namespace SS14.Client.Placement.Modes
{
    public class AlignSnapgridCenter : PlacementMode
    {
        public AlignSnapgridCenter(PlacementManager pMan) : base(pMan)
        {
        }

        public override bool Update(Vector2i mouseS, IMapManager currentMap)
        {
            if (currentMap == null) return false;

            mouseScreen = mouseS;
            mouseWorld = CluwneLib.ScreenToWorld(mouseScreen);
            
            if (!currentMap.TryFindGridAt(mouseWorld, out IMapGrid currentgrid)) //Cant find a grid
                return false;

            var mouselocal = currentgrid.WorldToLocal(mouseWorld); //Convert code to local grid coordinates
            var snapsize = currentgrid.SnapSize; //Find snap size
            mouselocal = new Vector2( //Round local coordinates onto the snap grid
                (float)(Math.Round((mouselocal.X*32 / (double)snapsize-0.5), MidpointRounding.AwayFromZero)+0.5) * snapsize / 32, 
                (float)(Math.Round((mouselocal.Y*32 / (double)snapsize-0.5), MidpointRounding.AwayFromZero)+0.5) * snapsize / 32);

            //Convert back to original world and screen coordinates after applying offset
            mouseWorld = currentgrid.LocalToWorld(mouselocal) + new Vector2(pManager.CurrentPrototype.PlacementOffset.X, pManager.CurrentPrototype.PlacementOffset.Y);
            mouseScreen = (Vector2i)CluwneLib.WorldToScreen(mouseWorld);

            if (!RangeCheck())
                return false;

            return true;
        }
    }
}
