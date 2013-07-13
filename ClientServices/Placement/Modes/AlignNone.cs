using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using ClientWindow;
using GorgonLibrary;
using ClientInterfaces.Map;
using GorgonLibrary.Graphics;
using SS13_Shared;

namespace ClientServices.Placement.Modes
{
    public class AlignNone : PlacementMode
    {
        public AlignNone(PlacementManager pMan) : base(pMan)
        {
        }

        public override bool Update(Vector2D mouseS, IMapManager currentMap)
        {
            if (currentMap == null) return false;

            spriteToDraw = GetDirectionalSprite(pManager.CurrentBaseSprite);

            mouseScreen = mouseS;
            mouseWorld = new Vector2D(mouseScreen.X + ClientWindowData.Singleton.ScreenOrigin.X, mouseScreen.Y + ClientWindowData.Singleton.ScreenOrigin.Y);

            var spriteRectWorld = new RectangleF(mouseWorld.X - (spriteToDraw.Width / 2f), mouseWorld.Y - (spriteToDraw.Height / 2f), spriteToDraw.Width, spriteToDraw.Height);

            if (pManager.CurrentPermission.IsTile)
                return false;

            if (pManager.CollisionManager.IsColliding(spriteRectWorld)) 
                return false;

            if (currentMap.IsSolidTile(mouseWorld)) return false;

            if (pManager.CurrentPermission.Range > 0)
                if ((pManager.PlayerManager.ControlledEntity.Position - mouseWorld).Length > pManager.CurrentPermission.Range) return false;

            currentTile = currentMap.GetTileAt(mouseWorld);

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
