using SS14.Shared.Maths;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Map;
using System.Drawing;
using SS14.Shared.IoC;

namespace SS14.Client.Services.Placement.Modes
{
    public class AlignFree : PlacementMode
    {
        public AlignFree(PlacementManager pMan)
            : base(pMan)
        {
        }

        public override bool Update(Vector2 mouseS, IMapManager currentMap)
        {
            if (currentMap == null) return false;

            spriteToDraw = GetDirectionalSprite(pManager.CurrentBaseSprite);

            int tileSize = IoCManager.Resolve<IMapManager>().TileSize;
            mouseScreen = mouseS;
            mouseWorld = mouseScreen / tileSize;
            currentTile = currentMap.GetTileRef(mouseWorld);

            return true;
        }

        public override void Render()
        {
            if (spriteToDraw != null)
            {
                spriteToDraw.Color = pManager.ValidPosition ? Color.ForestGreen.ToSFMLColor() :Color.IndianRed.ToSFMLColor();
                spriteToDraw.Position = new Vector2(mouseScreen.X - (spriteToDraw.Width/2f),
                                                     mouseScreen.Y - (spriteToDraw.Height/2f));
                //Centering the sprite on the cursor.
                spriteToDraw.Draw();
                spriteToDraw.Color = Color.White.ToSFMLColor();
            }
        }
    }
}