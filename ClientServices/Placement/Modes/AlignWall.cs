using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CGO;
using ClientInterfaces.Map;
using ClientWindow;
using GorgonLibrary;
using SS13_Shared.GO;

namespace ClientServices.Placement.Modes
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
            mouseWorld = new Vector2D(mouseScreen.X + ClientWindowData.Singleton.ScreenOrigin.X,
                                      mouseScreen.Y + ClientWindowData.Singleton.ScreenOrigin.Y);

            var spriteRectWorld = new RectangleF(mouseWorld.X - (spriteToDraw.Width/2f),
                                                 mouseWorld.Y - (spriteToDraw.Height/2f), spriteToDraw.Width,
                                                 spriteToDraw.Height);

            if (pManager.CurrentPermission.IsTile)
                return false;

            //CollisionManager collisionMgr = (CollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
            //if (collisionMgr.IsColliding(spriteRectWorld, true)) validPosition = false;

            if (!currentMap.IsSolidTile(mouseWorld))
                return false;

            if (pManager.CurrentPermission.Range > 0)
                if (
                    (pManager.PlayerManager.ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform)
                         .Position - mouseWorld).Length > pManager.CurrentPermission.Range)
                    return false;

            currentTile = currentMap.GetTileAt(mouseWorld);
            var nodes = new List<Vector2D>();

            if (pManager.CurrentTemplate.MountingPoints != null)
            {
                nodes.AddRange(
                    pManager.CurrentTemplate.MountingPoints.Select(
                        current => new Vector2D(mouseWorld.X, currentTile.Position.Y + current)));
            }
            else
            {
                nodes.Add(new Vector2D(mouseWorld.X, currentTile.Position.Y + 16));
                nodes.Add(new Vector2D(mouseWorld.X, currentTile.Position.Y + 32));
                nodes.Add(new Vector2D(mouseWorld.X, currentTile.Position.Y + 48));
            }

            Vector2D closestNode = (from Vector2D node in nodes
                                    orderby (node - mouseWorld).Length ascending
                                    select node).First();

            mouseWorld = Vector2D.Add(closestNode,
                                      new Vector2D(pManager.CurrentTemplate.PlacementOffset.Key,
                                                   pManager.CurrentTemplate.PlacementOffset.Value));
            mouseScreen = new Vector2D(mouseWorld.X - ClientWindowData.Singleton.ScreenOrigin.X,
                                       mouseWorld.Y - ClientWindowData.Singleton.ScreenOrigin.Y);

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