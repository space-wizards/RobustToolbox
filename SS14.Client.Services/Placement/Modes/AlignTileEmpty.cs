using SS14.Shared.Maths;

using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Map;
using SS14.Shared.GO;
using System.Drawing;
using SS14.Client.Graphics;
using SFML.System;
using SFML.Graphics;

namespace SS14.Client.Services.Placement.Modes
{
    public class AlignTileEmpty : PlacementMode
    {
        public AlignTileEmpty(PlacementManager pMan)
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

            currentTile = currentMap.GetTileRef(mouseWorld);

            if (currentTile.Tile.TileId != 0)
                return false;

            var rangeSquared = pManager.CurrentPermission.Range * pManager.CurrentPermission.Range;
            if (rangeSquared > 0)
                if (
                    (pManager.PlayerManager.ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform)
                         .Position - mouseWorld).LengthSquared() > rangeSquared)
                    return false;

            if (pManager.CurrentPermission.IsTile)
            {
                mouseWorld = new Vector2f(currentTile.X + 0.5f,
                                         currentTile.Y + 0.5f);
                mouseScreen = CluwneLib.WorldToScreen(mouseWorld).Round();
            }
            else
            {
                mouseWorld = new Vector2f(currentTile.X + 0.5f + pManager.CurrentTemplate.PlacementOffset.Key,
                                         currentTile.Y + 0.5f + pManager.CurrentTemplate.PlacementOffset.Value);
                mouseScreen = CluwneLib.WorldToScreen(mouseWorld).Round();

                spriteRectWorld = new FloatRect(mouseWorld.X - (bounds.Width/2f),
                                                 mouseWorld.Y - (bounds.Height/2f), bounds.Width,
                                                 bounds.Height);
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