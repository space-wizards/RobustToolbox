using GorgonLibrary;
using SS14.Client.ClientWindow;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Map;
using SS14.Shared;
using SS14.Shared.GO;
using System.Drawing;

namespace SS14.Client.Services.Placement.Modes
{
    public class AlignWallPlace : PlacementMode
    {
        public AlignWallPlace(PlacementManager pMan)
            : base(pMan)
        {
        }

        public override bool Update(Vector2D mouseS, IMapManager currentMap)
        {
            if (currentMap == null) return false;

            spriteToDraw = GetDirectionalSprite(pManager.CurrentBaseSprite);

            mouseScreen = mouseS;
            mouseWorld = new Vector2D(mouseScreen.X + ClientWindowData.Singleton.ScreenOrigin.X,
                                      mouseScreen.Y + ClientWindowData.Singleton.ScreenOrigin.Y);

            var spriteRectWorld = new RectangleF(mouseWorld.X - (spriteToDraw.Width / 2f),
                                                 mouseWorld.Y - (spriteToDraw.Height / 2f), spriteToDraw.Width,
                                                 spriteToDraw.Height);

            if (pManager.CurrentPermission.Range > 0)
                if (
                    (pManager.PlayerManager.ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform)
                         .Position - mouseWorld).Length > pManager.CurrentPermission.Range) return false;

            currentTile = currentMap.GetFloorAt(mouseWorld);

            if (currentTile == null)
                return false;

            if (pManager.Direction == Direction.East || pManager.Direction == Direction.West)
            {
                pManager.Direction = Direction.East; // Don't ask
                if (mouseWorld.Y > currentTile.Position.Y + (currentMap.GetTileSpacing() / 2f))
                {
                    mouseWorld = currentTile.Position + new Vector2D(0f, currentMap.GetTileSpacing() - (currentMap.GetWallThickness() / 2f));
                }
                else
                {
                    mouseWorld = currentTile.Position + new Vector2D(0f, -(currentMap.GetWallThickness() / 2f));
                }
            }
            else
            {
                pManager.Direction = Direction.North;
                if (mouseWorld.X > currentTile.Position.X + (currentMap.GetTileSpacing() / 2f))
                {
                    mouseWorld = currentTile.Position + new Vector2D(currentMap.GetTileSpacing() - (currentMap.GetWallThickness() / 2f), 0f);
                }
                else // Westside, yo
                {
                    mouseWorld = currentTile.Position + new Vector2D(-(currentMap.GetWallThickness() / 2f), 0f);
                }
            }

            mouseScreen = new Vector2D(mouseWorld.X - ClientWindowData.Singleton.ScreenOrigin.X,
                                             mouseWorld.Y - ClientWindowData.Singleton.ScreenOrigin.Y);

            return true;
        }

        public override void Render()
        {
            if (spriteToDraw != null)
            {
                spriteToDraw.Color = pManager.ValidPosition ? Color.ForestGreen : Color.IndianRed;
                spriteToDraw.Position = new Vector2D(mouseScreen.X,
                                                     mouseScreen.Y);
                //Centering the sprite on the cursor.
                spriteToDraw.Draw();
                spriteToDraw.Color = Color.White;
            }
        }
    }
}