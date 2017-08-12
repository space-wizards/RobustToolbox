using SFML.Graphics;
using SFML.System;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Map;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Maths;

namespace SS14.Client.Placement.Modes
{
    public class AlignTileAny : PlacementMode
    {
        public AlignTileAny(PlacementManager pMan)
            : base(pMan)
        {
        }

        public override bool Update(Vector2i mouseS, IMapManager currentMap)
        {
            if (currentMap == null) return false;

            spriteToDraw = GetSprite(pManager.CurrentBaseSpriteKey);

            mouseScreen = mouseS;
            mouseWorld = CluwneLib.ScreenToWorld(mouseScreen);

            var bounds = spriteToDraw.GetLocalBounds();

            currentTile = currentMap.GetTileRef(mouseWorld);

            //if (currentMap.IsSolidTile(mouseWorld)) validPosition = false;

            var rangeSquared = pManager.CurrentPermission.Range * pManager.CurrentPermission.Range;
            if (rangeSquared > 0)
                if (
                    (pManager.PlayerManager.ControlledEntity.GetComponent<ITransformComponent>()
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
                mouseWorld = new Vector2f(currentTile.X + 0.5f + pManager.CurrentPrototype.PlacementOffset.X,
                                         currentTile.Y + 0.5f + pManager.CurrentPrototype.PlacementOffset.Y);
                mouseScreen = CluwneLib.WorldToScreen(mouseWorld).Round();

                FloatRect spriteRectWorld = new FloatRect(mouseWorld.X - (bounds.Width/2f),
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
                spriteToDraw.Color = Color.White;
            }
        }
    }
}
