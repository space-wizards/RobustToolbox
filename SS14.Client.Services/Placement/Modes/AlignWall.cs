using GorgonLibrary;
using SS14.Client.ClientWindow;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Map;
using SS14.Shared.GO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SS14.Client.Services.Placement.Modes
{
    public class AlignWall : PlacementMode
    {
        public AlignWall(PlacementManager pMan)
            : base(pMan)
        {
        }

        public override bool Update(Vector2D mouseS, IMapManager currentMap)
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

            //CollisionManager collisionMgr = (CollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
            //if (collisionMgr.IsColliding(spriteRectWorld, true)) validPosition = false;

            currentTile = currentMap.GetTileRef(mouseWorld);

            if (!currentTile.Tile.TileDef.IsWall)
                return false;

            if (pManager.CurrentPermission.Range > 0)
                if (
                    (pManager.PlayerManager.ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform)
                         .Position - mouseWorld).Length > pManager.CurrentPermission.Range)
                    return false;

            var nodes = new List<Vector2D>();

            if (pManager.CurrentTemplate.MountingPoints != null)
            {
                nodes.AddRange(
                    pManager.CurrentTemplate.MountingPoints.Select(
                        current => new Vector2D(mouseWorld.X, currentTile.Y + current)));
            }
            else
            {
                nodes.Add(new Vector2D(mouseWorld.X, currentTile.Y + 0.5f));
                nodes.Add(new Vector2D(mouseWorld.X, currentTile.Y + 1.0f));
                nodes.Add(new Vector2D(mouseWorld.X, currentTile.Y + 1.5f));
            }

            Vector2D closestNode = (from Vector2D node in nodes
                                    orderby (node - mouseWorld).Length ascending
                                    select node).First();

            mouseWorld = Vector2D.Add(closestNode,
                                      new Vector2D(pManager.CurrentTemplate.PlacementOffset.Key,
                                                   pManager.CurrentTemplate.PlacementOffset.Value));
            mouseScreen = ClientWindowData.Singleton.WorldToScreen(mouseWorld);

            if (pManager.CurrentPermission.Range > 0)
                if (
                    (pManager.PlayerManager.ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform)
                         .Position - mouseWorld).Length > pManager.CurrentPermission.Range)
                    return false;

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