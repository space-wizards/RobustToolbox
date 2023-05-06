using System;
using System.Text;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    public abstract class DrawingHandleScreen : DrawingHandleBase
    {
        protected DrawingHandleScreen(Texture white) : base(white)
        {
        }

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
            => DrawString(font, pos, str, 1, color);

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

        public Vector2 DrawString(Font font, Vector2 pos, ReadOnlySpan<char> str, float scale, Color color)
        {
            var advanceTotal = Vector2.Zero;
            var baseLine = new Vector2(pos.X, font.GetAscent(scale) + pos.Y);
            var lineHeight = font.GetLineHeight(scale);

            foreach (var rune in str.EnumerateRunes())
            {
                if (rune == new Rune('\n'))
                {
                    baseLine.X = pos.X;
                    baseLine.Y += lineHeight;
                    advanceTotal.Y += lineHeight;
                    continue;
                }

                var advance = font.DrawChar(this, rune, baseLine, scale, color);
                advanceTotal.X += advance;
                baseLine += new Vector2(advance, 0);
            }

            return advanceTotal;
        }

        public Vector2 GetDimensions(Font font, ReadOnlySpan<char> str, float scale)
        {
            var baseLine = new Vector2(0f, font.GetAscent(scale));
            var lineHeight = font.GetLineHeight(scale);
            var advanceTotal = new Vector2(0f, lineHeight);

            foreach (var rune in str.EnumerateRunes())
            {
                if (rune == new Rune('\n'))
                {
                    baseLine.X = 0f;
                    baseLine.Y += lineHeight;
                    advanceTotal.Y += lineHeight;
                    continue;
                }

                var metrics = font.GetCharMetrics(rune, scale);

                if (metrics == null)
                    continue;

                var advance = metrics.Value.Advance;
                advanceTotal.X += advance;
                baseLine += new Vector2(advance, 0);
            }

            return advanceTotal;
        }

        /// <summary>
        /// Draws an entity.
        /// </summary>
        /// <param name="entity">The entity to draw</param>
        /// <param name="position">The local pixel position where the entity should be drawn.</param>
        /// <param name="scale">Scales the drawn entity</param>
        /// <param name="worldRot">The world rotation to use when drawing the entity.
        /// This impacts the sprites RSI direction. Null will retrieve the entity's actual rotation.
        /// </param>
        /// <param name="eyeRotation">The effective "eye" angle.
        /// This will cause the entity to be rotated, and may also affect the RSI directions.
        /// Draws the entity at some given angle.</param>
        /// <param name="overrideDirection">RSI direction override.</param>
        /// <param name="sprite">The entity's sprite component</param>
        /// <param name="xform">The entity's transform component.
        /// Only required if <see cref="overrideDirection"/> is null.</param>
        /// <param name="xformSystem">The transform system</param>
        public abstract void DrawEntity(EntityUid entity,
            Vector2 position,
            Vector2 scale,
            Angle? worldRot,
            Angle eyeRotation = default,
            Direction? overrideDirection = null,
            SpriteComponent? sprite = null,
            TransformComponent? xform = null,
            SharedTransformSystem? xformSystem = null);
    }
}
