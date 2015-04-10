using SS14.Client.Graphics;
using SS14.Shared.Maths;
using SS14.Client.ClientWindow;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Map;
using SS14.Shared.GO;
using System.Drawing;

namespace SS14.Client.Services.Placement.Modes
{
    public class AlignTileSolid : PlacementMode
    {
        public AlignTileSolid(PlacementManager pMan)
            : base(pMan)
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

            currentTile = currentMap.GetTileRef(mouseWorld);

            if (!currentTile.Tile.TileDef.IsWall)
                return false;

            if (pManager.CurrentPermission.Range > 0)
                if (
                    (pManager.PlayerManager.ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform)
                         .Position - mouseWorld).Length > pManager.CurrentPermission.Range)
                    return false;

            if (pManager.CurrentPermission.IsTile)
            {
                mouseWorld = new Vector2(currentTile.X + 0.5f,
                                         currentTile.Y + 0.5f);
                mouseScreen = ClientWindowData.Singleton.WorldToScreen(mouseWorld);
            }
            else
            {
                mouseWorld = new Vector2(currentTile.X + 0.5f + pManager.CurrentTemplate.PlacementOffset.Key,
                                         currentTile.Y + 0.5f + pManager.CurrentTemplate.PlacementOffset.Value);
                mouseScreen = ClientWindowData.Singleton.WorldToScreen(mouseWorld);

                spriteRectWorld = new RectangleF(mouseWorld.X - (spriteToDraw.Width/2f),
                                                 mouseWorld.Y - (spriteToDraw.Height/2f), spriteToDraw.Width,
                                                 spriteToDraw.Height);
                if (pManager.CollisionManager.IsColliding(spriteRectWorld))
                    return false;
                //Since walls also have collisions, this means we can't place objects on walls with this mode.
            }

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