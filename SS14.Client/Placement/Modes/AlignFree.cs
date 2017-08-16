using SFML.System;
using SS14.Client.Graphics;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Utility;

namespace SS14.Client.Placement.Modes
{
    public class AlignFree : PlacementMode
    {
        public AlignFree(PlacementManager pMan) : base(pMan)
        {
        }
        
        public override bool Update(Vector2i mouseS, IMapManager currentMap)
        {
            if (currentMap == null) return false;

            spriteToDraw = GetDirectionalSprite(pManager.CurrentBaseSpriteKey);

            mouseScreen = mouseS;
            mouseWorld = CluwneLib.ScreenToWorld(mouseScreen);
            currentTile = currentMap.GetDefaultGrid().GetTile(mouseWorld.Convert());

            return true;
        }
    }
}