using SFML.Graphics;
using SFML.System;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics
{
    public static class SpriteExt
    {
        public static void SetTransformToRect(this SFML.Graphics.Sprite sprite, Box2i rect)
        {
            sprite.Scale = new Vector2f(rect.Width / (float) sprite.TextureRect.Width, rect.Height / (float) sprite.TextureRect.Height);
            sprite.Position = new Vector2f(rect.Left, rect.Top);
        }

        public static void Draw(this SFML.Graphics.Sprite sprite)
        {
            sprite.Draw(CluwneLib.CurrentRenderTarget, new RenderStates(CluwneLib.CurrentShader));
        }
    }
}
