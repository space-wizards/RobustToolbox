using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using CGO;
using ClientWindow;
using GorgonLibrary;
using ClientInterfaces.Map;
using GorgonLibrary.Graphics;
using SS13_Shared;
using SS13_Shared.GO;

namespace ClientServices.Placement.Modes
{
    public class AlignTileAny : PlacementMode
    {
        public AlignTileAny(PlacementManager pMan)
            : base(pMan)
        {
        }

        public override bool Update(Vector2D mouseS, IMapManager currentMap)
        {
            if (currentMap == null) return false;

            spriteToDraw = GetDirectionalSprite(pManager.CurrentBaseSprite);

            mouseScreen = mouseS;
            mouseWorld = new Vector2D(mouseScreen.X + ClientWindowData.Singleton.ScreenOrigin.X, mouseScreen.Y + ClientWindowData.Singleton.ScreenOrigin.Y);

            var spriteRectWorld = new RectangleF(mouseWorld.X - (spriteToDraw.Width / 2f), mouseWorld.Y - (spriteToDraw.Height / 2f), spriteToDraw.Width, spriteToDraw.Height);

            currentTile = currentMap.GetTileAt(mouseWorld);

            //if (currentMap.IsSolidTile(mouseWorld)) validPosition = false;

            if (pManager.CurrentPermission.Range > 0)
                if ((pManager.PlayerManager.ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform).Position - mouseWorld).Length > pManager.CurrentPermission.Range) 
                    return false;

            if (currentTile != null)
            {
                if (pManager.CurrentPermission.IsTile)
                {
                    mouseWorld = (currentTile.Position + new Vector2D(currentMap.GetTileSpacing() / 2f, currentMap.GetTileSpacing() / 2f));
                    mouseScreen = new Vector2D(mouseWorld.X - ClientWindowData.Singleton.ScreenOrigin.X, mouseWorld.Y - ClientWindowData.Singleton.ScreenOrigin.Y);

                }
                else
                {
                    mouseWorld = (currentTile.Position + new Vector2D(currentMap.GetTileSpacing() / 2f, currentMap.GetTileSpacing() / 2f)) + new Vector2D(pManager.CurrentTemplate.PlacementOffset.Key, pManager.CurrentTemplate.PlacementOffset.Value);
                    mouseScreen = new Vector2D(mouseWorld.X - ClientWindowData.Singleton.ScreenOrigin.X, mouseWorld.Y - ClientWindowData.Singleton.ScreenOrigin.Y);

                    spriteRectWorld = new RectangleF(mouseWorld.X - (spriteToDraw.Width / 2f), mouseWorld.Y - (spriteToDraw.Height / 2f), spriteToDraw.Width, spriteToDraw.Height);
                    if (pManager.CollisionManager.IsColliding(spriteRectWorld)) return false; //Since walls also have collisions, this means we can't place objects on walls with this mode.
                }
            }

            return true; 
        }

        public override void Render()
        {
            if (spriteToDraw != null)
            {
                spriteToDraw.Color = pManager.ValidPosition ? Color.ForestGreen : Color.IndianRed;
                spriteToDraw.Position = new Vector2D(mouseScreen.X - (spriteToDraw.Width / 2f), mouseScreen.Y - (spriteToDraw.Height / 2f)); //Centering the sprite on the cursor.
                spriteToDraw.Draw();
                spriteToDraw.Color = Color.White;
            }
        }
    }
}
