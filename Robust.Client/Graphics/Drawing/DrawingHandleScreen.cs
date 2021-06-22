using System;
using System.Text;
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

        /// <summary>
        ///     Draw a simple string to the screen at the specified position.
        /// </summary>
        /// <remarks>
        ///     This method is primarily intended for debug purposes and does not handle things like UI scaling.
        /// </remarks>
        /// <returns>
        ///     The space taken up (horizontal and vertical) by the text.
        /// </returns>
        /// <param name="font">The font to render with.</param>
        /// <param name="pos">The top-left corner to start drawing text at.</param>
        /// <param name="str">The text to draw.</param>
        /// <param name="color">The color of text to draw.</param>
        public Vector2 DrawString(Font font, Vector2 pos, string str, Color color)
        {
            var advanceTotal = Vector2.Zero;
            var baseLine = new Vector2(pos.X, font.GetAscent(1) + pos.Y);
            var lineHeight = font.GetLineHeight(1);

            foreach (var rune in str.EnumerateRunes())
            {
                if (rune == new Rune('\n'))
                {
                    baseLine.X = pos.X;
                    baseLine.Y += lineHeight;
                    advanceTotal.Y += lineHeight;
                    continue;
                }

                var advance = font.DrawChar(this, rune, baseLine, 1, color);
                advanceTotal.X += advance;
                baseLine += new Vector2(advance, 0);
            }

            return advanceTotal;
        }

        /// <summary>
        ///     Draw a simple string to the screen at the specified position.
        /// </summary>
        /// <remarks>
        ///     This method is primarily intended for debug purposes and does not handle things like UI scaling.
        /// </remarks>
        /// <returns>
        ///     The space taken up (horizontal and vertical) by the text.
        /// </returns>
        /// <param name="font">The font to render with.</param>
        /// <param name="pos">The top-left corner to start drawing text at.</param>
        /// <param name="str">The text to draw.</param>
        public Vector2 DrawString(Font font, Vector2 pos, string str)
            => DrawString(font, pos, str, Color.White);
    }
}
