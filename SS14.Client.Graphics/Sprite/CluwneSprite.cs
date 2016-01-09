using SFML.Graphics;
using SFML.System;

namespace SS14.Client.Graphics
{
    public static class SpriteExt
    {
        public static void SetTransformToRect(this SFML.Graphics.Sprite sprite, IntRect rect)
        {
            sprite.Scale = new SFML.System.Vector2f((float)rect.Width / (float)sprite.TextureRect.Width, (float)rect.Height / (float)sprite.TextureRect.Height);
            sprite.Position = new Vector2f(rect.Left, rect.Top);
        }

        public static void Draw(this SFML.Graphics.Sprite sprite)
        {
            sprite.Draw(CluwneLib.CurrentRenderTarget, new RenderStates(CluwneLib.CurrentShader));
        }
    }
}
