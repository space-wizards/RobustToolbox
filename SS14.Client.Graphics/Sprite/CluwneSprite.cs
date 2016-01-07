using SFML.Graphics;
using SFML.System;
using SS14.Shared.Maths;
using System.Drawing;

namespace SS14.Client.Graphics
{
    public static class SpriteExt
    {
        public static void SetTransformToRect(this SFML.Graphics.Sprite sprite, Rectangle rect)
        {
            sprite.Scale = new Vector2f((float)rect.Width / (float)sprite.TextureRect.Width, (float)rect.Height / (float)sprite.TextureRect.Height);
            sprite.Position = new Vector2(rect.X, rect.Y);
        }

        public static void Draw(this SFML.Graphics.Sprite sprite)
        {
            sprite.Draw(CluwneLib.CurrentRenderTarget, new RenderStates(CluwneLib.CurrentShader));
        }
    }
}
