using System.Drawing;
using CGO;
using ClientInterfaces.Map;
using ClientWindow;
using GorgonLibrary;
using SS13_Shared;
using SS13_Shared.GO;

namespace ClientServices.Placement.Modes
{
    public class AlignWallTops : PlacementMode
    {
        public AlignWallTops(PlacementManager pMan)
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

            var spriteRectWorld = new RectangleF(mouseWorld.X - (spriteToDraw.Width/2f),
                                                 mouseWorld.Y - (spriteToDraw.Height/2f), spriteToDraw.Width,
                                                 spriteToDraw.Height);

            bool ValidPosition = false;

            if (pManager.CurrentPermission.Range > 0)
                if (
                    (pManager.PlayerManager.ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform)
                         .Position - mouseWorld).Length > pManager.CurrentPermission.Range) return false;

            if (pManager.CurrentPermission.IsTile)
                return false;

            currentTile = currentMap.GetWallAt(mouseWorld);

            if (currentTile == null || !currentTile.IsSolidTile())
                return false;

            ITile wallTop = currentMap.GetWallAt(new Vector2D(currentTile.Position.X, currentTile.Position.Y - currentMap.GetTileSpacing()));
            if (wallTop == null)
                return false;

            ITile wallTopNorth = currentMap.GetWallAt(new Vector2D(wallTop.Position.X, wallTop.Position.Y - currentMap.GetTileSpacing()));
            ITile wallCurrentSouth = currentMap.GetWallAt(new Vector2D(currentTile.Position.X, currentTile.Position.Y - currentMap.GetTileSpacing()));

            ITile wallTopEast = currentMap.GetWallAt(new Vector2D(wallTop.Position.X + currentMap.GetTileSpacing(), wallTop.Position.Y));
            ITile wallCurrentEast = currentMap.GetWallAt(new Vector2D(currentTile.Position.X + currentMap.GetTileSpacing(), currentTile.Position.Y));

            ITile wallTopWest = currentMap.GetWallAt(new Vector2D(wallTop.Position.X - currentMap.GetTileSpacing(), wallTop.Position.Y));
            ITile wallCurrentWest = currentMap.GetWallAt(new Vector2D(currentTile.Position.X - currentMap.GetTileSpacing(), currentTile.Position.Y));

            switch (pManager.Direction)
            {
                case Direction.North:
                    if (wallTopNorth == null || wallTopNorth.IsSolidTile() || wallTop.IsSolidTile())
                        return false;
                    else
                    {
                        mouseWorld = new Vector2D(wallTop.Position.X + currentMap.GetTileSpacing()/2f,
                                                  wallTop.Position.Y - spriteToDraw.AABB.Height);
                        ValidPosition = true;
                    }
                    break;
                case Direction.East:
                    if (wallTopEast == null || wallTopEast.IsSolidTile() || wallCurrentEast.IsSolidTile())
                        return false;
                    else
                    {
                        mouseWorld =
                            new Vector2D(wallTop.Position.X + spriteToDraw.AABB.Width + currentMap.GetTileSpacing(),
                                         wallTop.Position.Y + currentMap.GetTileSpacing()/2f);
                        ValidPosition = true;
                    }
                    break;
                case Direction.South:
                    if (!currentTile.IsSolidTile() || wallCurrentSouth.IsSolidTile())
                        return false;
                    else
                    {
                        mouseWorld = new Vector2D(wallTop.Position.X + currentMap.GetTileSpacing()/2f,
                                                  wallTop.Position.Y + spriteToDraw.AABB.Height +
                                                  currentMap.GetTileSpacing());
                        ValidPosition = true;
                    }
                    break;
                case Direction.West:
                    if (wallTopWest == null || wallTopWest.IsSolidTile() || wallCurrentWest.IsSolidTile())
                        return false;
                    else
                    {
                        mouseWorld = new Vector2D(wallTop.Position.X - spriteToDraw.AABB.Width,
                                                  wallTop.Position.Y + currentMap.GetTileSpacing()/2f);
                        ValidPosition = true;
                    }
                    break;
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
                spriteToDraw.Position = new Vector2D(mouseScreen.X - (spriteToDraw.Width/2f),
                                                     mouseScreen.Y - (spriteToDraw.Height/2f));
                //Centering the sprite on the cursor.
                spriteToDraw.Draw();
                spriteToDraw.Color = Color.White;
            }
        }
    }
}