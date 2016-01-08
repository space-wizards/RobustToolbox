using SS14.Shared.Maths;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Map;
using SS14.Shared.GO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SS14.Client.Graphics;
using SFML.System;
using SFML.Graphics;

namespace SS14.Client.Services.Placement.Modes
{
    public class AlignWall : PlacementMode
    {
        public AlignWall(PlacementManager pMan)
            : base(pMan)
        {
        }

        public override bool Update(Vector2i mouseS, IMapManager currentMap)
        {
            if (currentMap == null) return false;

            spriteToDraw = GetDirectionalSprite(pManager.CurrentBaseSpriteKey);

            mouseScreen = mouseS;
            mouseWorld = CluwneLib.ScreenToWorld(mouseScreen);

            var bounds = spriteToDraw.GetLocalBounds();
            var spriteSize = CluwneLib.PixelToTile(new Vector2f(bounds.Width, bounds.Height));
            var spriteRectWorld = new FloatRect(mouseWorld.X - (spriteSize.X / 2f),
                                                 mouseWorld.Y - (spriteSize.Y / 2f),
                                                 spriteSize.X, spriteSize.Y);

            if (pManager.CurrentPermission.IsTile)
                return false;

            //CollisionManager collisionMgr = (CollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
            //if (collisionMgr.IsColliding(spriteRectWorld, true)) validPosition = false;

            currentTile = currentMap.GetTileRef(mouseWorld);

            if (!currentTile.Tile.TileDef.IsWall)
                return false;

            var rangeSquared = pManager.CurrentPermission.Range * pManager.CurrentPermission.Range;
            if (rangeSquared > 0)
                if (
                    (pManager.PlayerManager.ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform)
                         .Position - mouseWorld).LengthSquared() > rangeSquared)
                    return false;

            var nodes = new List<Vector2f>();

            if (pManager.CurrentTemplate.MountingPoints != null)
            {
                nodes.AddRange(
                    pManager.CurrentTemplate.MountingPoints.Select(
                        current => new Vector2f(mouseWorld.X, currentTile.Y + current)));
            }
            else
            {
                nodes.Add(new Vector2f(mouseWorld.X, currentTile.Y + 0.5f));
                nodes.Add(new Vector2f(mouseWorld.X, currentTile.Y + 1.0f));
                nodes.Add(new Vector2f(mouseWorld.X, currentTile.Y + 1.5f));
            }

            Vector2f closestNode = (from Vector2f node in nodes
                                    orderby (node - mouseWorld).LengthSquared() ascending
                                    select node).First();

            mouseWorld = closestNode + new Vector2f(pManager.CurrentTemplate.PlacementOffset.Key,
                                                    pManager.CurrentTemplate.PlacementOffset.Value);
            mouseScreen = CluwneLib.WorldToScreen(mouseWorld).Round();

            var range = pManager.CurrentPermission.Range;
            if (range > 0)
                if (
                    (pManager.PlayerManager.ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform)
                         .Position - mouseWorld).LengthSquared() > range * range)
                    return false;

            return true;
        }

        public override void Render()
        {
            if (spriteToDraw != null)
            {
                var bounds = spriteToDraw.GetLocalBounds();
                spriteToDraw.Color = pManager.ValidPosition ? new SFML.Graphics.Color(34, 139, 34) : new SFML.Graphics.Color(205, 92, 92);
                spriteToDraw.Position = new Vector2f(mouseScreen.X - (bounds.Width/2f),
                                                     mouseScreen.Y - (bounds.Height/2f));
                //Centering the sprite on the cursor.
                spriteToDraw.Draw();
                spriteToDraw.Color = SFML.Graphics.Color.White;
            }
        }
    }
}