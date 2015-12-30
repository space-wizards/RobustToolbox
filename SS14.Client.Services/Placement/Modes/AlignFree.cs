using SS14.Shared.Maths;
using SS14.Client.Interfaces.Map;
using System.Drawing;
using SS14.Client.Graphics;
using Color = SFML.Graphics.Color;
using SFML.Graphics;

namespace SS14.Client.Services.Placement.Modes
{
    public class AlignFree : PlacementMode
    {
        public AlignFree(PlacementManager pMan) : base(pMan)
        {
        }

        public override bool Update(Vector2 mouseS, IMapManager currentMap)
        {
            if (currentMap == null) return false;

            spriteToDraw = GetDirectionalSprite(pManager.CurrentBaseSpriteKey);

            mouseScreen = mouseS;
            mouseWorld = CluwneLib.ScreenToWorld(mouseScreen);
            currentTile = currentMap.GetTileRef(mouseWorld);

            return true;
        }

        public override void Render()
        {
            if (spriteToDraw != null)
            {
                var bounds = spriteToDraw.GetLocalBounds();
                spriteToDraw.Color = pManager.ValidPosition ? new Color(34, 34, 139) : new Color(205, 92, 92);
                spriteToDraw.Position = new Vector2(mouseScreen.X - (bounds.Width/2f),
                                                     mouseScreen.Y - (bounds.Height/2f));
                //Centering the sprite on the cursor.
                spriteToDraw.Draw(CluwneLib.CurrentRenderTarget, RenderStates.Default);
                spriteToDraw.Color = Color.White;
            }
        }
    }
}