using SS14.Client.Graphics;
using SS14.Shared.Maths;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Map;
using SS14.Shared.GO;
using System.Drawing;
using Color = SFML.Graphics.Color;

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

            spriteToDraw = GetDirectionalSprite(pManager.CurrentBaseSpriteKey);

            mouseScreen = mouseS;
            mouseWorld = CluwneLib.ScreenToWorld(mouseScreen);

            var bounds = spriteToDraw.GetLocalBounds();
            var spriteSize = CluwneLib.PixelToTile(new SizeF(bounds.Width, bounds.Height));
            var spriteRectWorld = new RectangleF(mouseWorld.X - (spriteSize.Width / 2f),
                                                 mouseWorld.Y - (spriteSize.Height / 2f),
                                                 spriteSize.Width, spriteSize.Height);

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
                mouseScreen = CluwneLib.WorldToScreen(mouseWorld);
            }
            else
            {
                mouseWorld = new Vector2(currentTile.X + 0.5f + pManager.CurrentTemplate.PlacementOffset.Key,
                                         currentTile.Y + 0.5f + pManager.CurrentTemplate.PlacementOffset.Value);
                mouseScreen = CluwneLib.WorldToScreen(mouseWorld);

                spriteRectWorld = new RectangleF(mouseWorld.X - (bounds.Width/2f),
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
                spriteToDraw.Position = new Vector2(mouseScreen.X - (bounds.Width/2f),
                                                    mouseScreen.Y - (bounds.Height/2f));
                //Centering the sprite on the cursor.
                spriteToDraw.Draw();
                spriteToDraw.Color = Color.White;
            }
        }
    }
}