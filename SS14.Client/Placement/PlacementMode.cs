using System.IO;
using SS14.Client.Graphics;
using SS14.Client.Graphics.ClientEye;
using SS14.Client.ResourceManagement;
using SS14.Client.Utility;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.Placement
{
    public class PlacementMode
    {
        public readonly PlacementManager pManager;
        public TileRef CurrentTile { get; set; }
        public ScreenCoordinates MouseScreen { get; set; }
        public LocalCoordinates MouseCoords { get; set; }
        public TextureResource SpriteToDraw { get; set; }
        public Color ValidPlaceColor { get; set; } = new Color(34, 139, 34); //Default valid color is green
        public Color InvalidPlaceColor { get; set; } = new Color(34, 34, 139); //Default invalid placement is red

        public virtual bool rangerequired => false;

        public PlacementMode(PlacementManager pMan)
        {
            pManager = pMan;
        }

        public virtual string ModeName => GetType().Name;

        public virtual bool FrameUpdate(RenderFrameEventArgs e, ScreenCoordinates mouseScreen)
        {
            return false;
        }

        public virtual void Render()
        {
            if (SpriteToDraw == null)
            {
                SetSprite();
            }

            var size = SpriteToDraw.Texture.Size;
            var color = pManager.ValidPosition ? ValidPlaceColor : InvalidPlaceColor;
            var pos = MouseCoords.Position * EyeManager.PIXELSPERMETER - size / 2f;
            pManager.drawNode.DrawTexture(SpriteToDraw.Texture.GodotTexture, pos.Convert(), color.Convert());
        }

        public TextureResource GetSprite(string key)
        {
            return pManager.ResourceCache.GetResource<TextureResource>("Textures/" + key);
        }

        public bool TryGetSprite(string key, out TextureResource sprite)
        {
            return pManager.ResourceCache.TryGetResource<TextureResource>("Textures/" + key, out sprite);
        }

        public void SetSprite()
        {
            SpriteToDraw = GetDirectionalSprite(pManager.CurrentBaseSpriteKey);
        }

        public TextureResource GetDirectionalSprite(string baseSprite)
        {
            var ext = Path.GetExtension(baseSprite);
            var withoutExt = Path.ChangeExtension(baseSprite, null);
            var name = $"{withoutExt}_{pManager.Direction.ToString().ToLowerInvariant()}{ext}";

            if (TryGetSprite(name, out var sprite))
            {
                return sprite;
            }

            return GetSprite(baseSprite);
        }

        public bool RangeCheck()
        {
            if (!rangerequired)
                return true;
            var range = pManager.CurrentPermission.Range;
            if (range > 0 && !pManager.PlayerManager.LocalPlayer.ControlledEntity.GetComponent<ITransformComponent>().LocalPosition.InRange(MouseCoords, range))
                return false;
            return true;
        }

        public bool CheckCollision()
        {
            return true;
            /*
            var drawsprite = GetSprite(pManager.CurrentBaseSpriteKey);
            var bounds = drawsprite.LocalBounds;
            var spriteSize = CluwneLib.PixelToTile(new Vector2(bounds.Width, bounds.Height));
            var spriteRectWorld = Box2.FromDimensions(MouseCoords.X - (spriteSize.X / 2f),
                                                 MouseCoords.Y - (spriteSize.Y / 2f),
                                                 spriteSize.X, spriteSize.Y);
            if (pManager.CollisionManager.IsColliding(spriteRectWorld))
                return false;
            return true;
            */
        }

        protected Vector2 ScreenToWorld(Vector2 point)
        {
            return pManager.eyeManager.ScreenToWorld(point);
        }

        protected Vector2 WorldToScreen(Vector2 point)
        {
            return pManager.eyeManager.WorldToScreen(point);
        }
    }
}

