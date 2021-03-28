using System;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    public abstract class DrawingHandleScreen : DrawingHandleBase
    {
        public abstract void DrawRect(UIBox2 rect, Color color, bool filled = true);

        public abstract void DrawTextureRectRegion(Texture texture, UIBox2 rect, UIBox2? subRegion = null, Color? modulate = null);

        public void DrawTexture(Texture texture, Vector2 position, Color? modulate = null)
        {
            CheckDisposed();

            DrawTextureRect(texture, UIBox2.FromDimensions(position, texture.Size), modulate);
        }

        public void DrawTextureRect(Texture texture, UIBox2 rect, Color? modulate = null)
        {
            CheckDisposed();

            DrawTextureRectRegion(texture, rect, null, modulate);
        }

        public override void DrawCircle(Vector2 position, float radius, Color color, bool filled = true)
        {
            const int segments = 64;
            Span<Vector2> buffer = stackalloc Vector2[segments + 1];

            for (var i = 0; i <= segments; i++)
            {
                var angle = i / (float) segments * MathHelper.TwoPi;
                var pos = new Vector2(MathF.Sin(angle), MathF.Cos(angle));

                buffer[i] = position + pos * radius;
            }

            DrawPrimitiveTopology type = filled ? DrawPrimitiveTopology.TriangleFan : DrawPrimitiveTopology.LineStrip;
            DrawPrimitives(type, buffer, color);
        }
    }
}
