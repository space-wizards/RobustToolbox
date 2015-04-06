using SS14.Shared.Maths;
using SS14.Client.ClientWindow;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Map;
using SS14.Shared.GO;
using System.Drawing;
using SS14.Client.Graphics.CluwneLib;

namespace SS14.Client.Services.Placement.Modes
{
    public class AlignTileNonSolid : PlacementMode
    {
        public AlignTileNonSolid(PlacementManager pMan)
            : base(pMan)
        {
        }

        public override bool Update(Vector2 mouseS, IMapManager currentMap)
        {
            if (currentMap == null) return false;

            spriteToDraw = GetDirectionalSprite(pManager.CurrentBaseSprite);

            mouseScreen = mouseS;
            mouseWorld = new Vector2(mouseScreen.X + ClientWindowData.Singleton.ScreenOrigin.X,
                                      mouseScreen.Y + ClientWindowData.Singleton.ScreenOrigin.Y);

            var spriteRectWorld = new RectangleF(mouseWorld.X - (spriteToDraw.Width/2f),
                                                 mouseWorld.Y - (spriteToDraw.Height/2f), spriteToDraw.Width,
                                                 spriteToDraw.Height);

            currentTile = currentMap.GetFloorAt(mouseWorld);

            if (currentMap.IsSolidTile(mouseWorld))
                return false;

            if (pManager.CurrentPermission.Range > 0)
                if (
                    (pManager.PlayerManager.ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform)
                         .Position - mouseWorld).Length > pManager.CurrentPermission.Range)
                    return false;

            if (currentTile != null)
            {
                if (pManager.CurrentPermission.IsTile)
                {
                    mouseWorld = (currentTile.Position +
                                  new Vector2(currentMap.GetTileSpacing()/2f, currentMap.GetTileSpacing()/2f));
                    mouseScreen = new Vector2(mouseWorld.X - ClientWindowData.Singleton.ScreenOrigin.X,
                                               mouseWorld.Y - ClientWindowData.Singleton.ScreenOrigin.Y);
                }
                else
                {
                    mouseWorld = (currentTile.Position +
                                  new Vector2(currentMap.GetTileSpacing()/2f, currentMap.GetTileSpacing()/2f)) +
                                 new Vector2(pManager.CurrentTemplate.PlacementOffset.Key,
                                              pManager.CurrentTemplate.PlacementOffset.Value);
                    mouseScreen = new Vector2(mouseWorld.X - ClientWindowData.Singleton.ScreenOrigin.X,
                                               mouseWorld.Y - ClientWindowData.Singleton.ScreenOrigin.Y);

                    spriteRectWorld = new RectangleF(mouseWorld.X - (spriteToDraw.Width/2f),
                                                     mouseWorld.Y - (spriteToDraw.Height/2f), spriteToDraw.Width,
                                                     spriteToDraw.Height);
                    if (pManager.CollisionManager.IsColliding(spriteRectWorld))
                        return false;
                    //Since walls also have collisions, this means we can't place objects on walls with this mode.
                }
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