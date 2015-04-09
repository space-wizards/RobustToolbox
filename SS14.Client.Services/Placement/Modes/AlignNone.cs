using SS14.Shared.Maths;
using SS14.Client.ClientWindow;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Map;
using SS14.Shared.GO;
using System.Drawing;
using SS14.Client.Graphics.CluwneLib;

namespace SS14.Client.Services.Placement.Modes
{
    public class AlignNone : PlacementMode
    {
        public AlignNone(PlacementManager pMan) : base(pMan)
        {
        }

        public override bool Update(Vector2 mouseS, IMapManager currentMap)
        {
            if (currentMap == null) return false;

            spriteToDraw = GetDirectionalSprite(pManager.CurrentBaseSprite);

            mouseScreen = mouseS;
            mouseWorld = ClientWindowData.Singleton.ScreenToWorld(mouseScreen);

            var spriteSize = ClientWindowData.Singleton.PixelToTile(spriteToDraw.Size);
            var spriteRectWorld = new RectangleF(mouseWorld.X - (spriteSize.X / 2f),
                                                 mouseWorld.Y - (spriteSize.Y / 2f),
                                                 spriteSize.X, spriteSize.Y);

            if (pManager.CurrentPermission.IsTile)
                return false;

            if (pManager.CollisionManager.IsColliding(spriteRectWorld))
                return false;

            //if (currentMap.IsSolidTile(mouseWorld)) return false;

            if (pManager.CurrentPermission.Range > 0)
                if (
                    (pManager.PlayerManager.ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform)
                         .Position - mouseWorld).Length > pManager.CurrentPermission.Range) return false;

            currentTile = currentMap.GetTileRef(mouseWorld);

            return true;
        }

        public override void Render()
        {
            if (spriteToDraw != null)
            {
                spriteToDraw.Color = pManager.ValidPosition ? CluwneLib.SystemColorToSFML(Color.ForestGreen) : CluwneLib.SystemColorToSFML(Color.IndianRed);
                spriteToDraw.Position = new Vector2(mouseScreen.X - (spriteToDraw.Width/2f),
                                                     mouseScreen.Y - (spriteToDraw.Height/2f));
                //Centering the sprite on the cursor.
                spriteToDraw.Draw();
                spriteToDraw.Color = CluwneLib.SystemColorToSFML(Color.White);
            }
        }
    }
}