using OpenTK;
using SFML.Graphics;
using SFML.System;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.Placement.Modes
{
    public class AlignNone : PlacementMode
    {
        public AlignNone(PlacementManager pMan) : base(pMan)
        {
        }

        public override bool Update(Vector2i mouseS, IMapManager currentMap)
        {
            if (currentMap == null) return false;

            spriteToDraw = GetDirectionalSprite(pManager.CurrentBaseSpriteKey);

            mouseScreen = mouseS;
            mouseWorld = CluwneLib.ScreenToWorld(mouseScreen);

            var bounds = spriteToDraw.GetLocalBounds();
            var spriteSize = CluwneLib.PixelToTile(new Vector2(bounds.Width, bounds.Height));
            var spriteRectWorld = Box2.FromDimensions(mouseWorld.X - (spriteSize.X / 2f),
                                                 mouseWorld.Y - (spriteSize.Y / 2f),
                                                 spriteSize.X, spriteSize.Y);

            if (pManager.CurrentPermission.IsTile)
                return false;

            if (pManager.CollisionManager.IsColliding(spriteRectWorld))
                return false;

            var rangeSquared = pManager.CurrentPermission.Range * pManager.CurrentPermission.Range;
            if (rangeSquared > 0)
                if ((pManager.PlayerManager.ControlledEntity.GetComponent<ITransformComponent>()
                         .Position - mouseWorld).LengthSquared > rangeSquared) return false;

            currentTile = currentMap.GetDefaultGrid().GetTile(mouseWorld);

            return true;
        }
    }
}
