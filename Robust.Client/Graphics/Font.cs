using System;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Interfaces.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    /// <summary>
    ///     A generic font for rendering of text.
    ///     Does not contain properties such as size. Those are specific to children such as <see cref="VectorFont" />
    /// </summary>
    public abstract class Font
    {
        /// <summary>
        ///     The maximum amount a glyph goes above the baseline, in pixels.
        /// </summary>
        public abstract int GetAscent(float scale);

        /// <summary>
        ///     The maximum glyph height of a line of text in pixels, not relative to the baseline.
        /// </summary>
        public abstract int GetHeight(float scale);

        /// <summary>
        ///     The maximum amount a glyph drops below the baseline, in pixels.
        /// </summary>
        public abstract int GetDescent(float scale);

        /// <summary>
        ///     The distance between the baselines of two consecutive lines, in pixels.
        ///     Basically, if you encounter a new line, this is how much you need to move down the cursor.
        /// </summary>
        public abstract int GetLineHeight(float scale);

        /// <summary>
        ///     The distance between the edges of two consecutive lines, in pixels.
        /// </summary>
        public int GetLineSeparation(float scale)
        {
            return GetLineHeight(scale) - GetHeight(scale);
        }

        [Obsolete("Use GetAscent")] public int Ascent => GetAscent(1);
        [Obsolete("Use GetHeight")] public int Height => GetHeight(1);
        [Obsolete("Use GetDescent")] public int Descent => GetDescent(1);
        [Obsolete("Use GetLineHeight")] public int LineHeight => GetLineHeight(1);
        [Obsolete("Use GetLineSeparation")] public int LineSeparation => GetLineSeparation(1);

        // Yes, I am aware that using char is bad.
        // At the same time the font system is nowhere close to rendering Unicode so...
        /// <summary>
        ///     Draw a character at a certain baseline position on screen.
        /// </summary>
        /// <param name="handle">The drawing handle to draw to.</param>
        /// <param name="chr">
        ///     The Unicode code point to draw. Yes I'm aware about UTF-16 being crap,
        ///     do you think this system can draw anything except ASCII?
        /// </param>
        /// <param name="baseline">The baseline from which to draw the character.</param>
        /// <param name="color">The color of the character to draw.</param>
        /// <returns>How much to advance the cursor to draw the next character.</returns>
        [Obsolete("Use DrawChar with scale support.")]
        public float DrawChar(DrawingHandleScreen handle, char chr, Vector2 baseline, Color color)
        {
            return DrawChar(handle, chr, baseline, 1, color);
        }

        public abstract float DrawChar(DrawingHandleScreen handle, char chr, Vector2 baseline, float scale,
            Color color);

        /// <summary>
        ///     Gets metrics describing the dimensions and positioning of a single glyph in the font.
        /// </summary>
        /// <param name="chr">The character to fetch the glyph metrics for.</param>
        /// <returns>
        ///     <c>null</c> if this font does not have a glyph for the specified character,
        ///     otherwise the metrics you asked for.
        /// </returns>
        /// <seealso cref="TryGetCharMetrics"/>
        [Obsolete("Use GetCharMetrics with scale support.")]
        public CharMetrics? GetCharMetrics(char chr)
        {
            return GetCharMetrics(chr, 1);
        }

        public abstract CharMetrics? GetCharMetrics(char chr, float scale);

        /// <summary>
        ///     Try-pattern version of <see cref="GetCharMetrics"/>.
        /// </summary>
        [Obsolete("Use TryGetCharMetrics with scale support.")]
        public bool TryGetCharMetrics(char chr, out CharMetrics metrics)
        {
            return TryGetCharMetrics(chr, 1, out metrics);
        }

        public bool TryGetCharMetrics(char chr, float scale, out CharMetrics metrics)
        {
            var maybe = GetCharMetrics(chr, scale);
            if (maybe.HasValue)
            {
                metrics = maybe.Value;
                return true;
            }

            metrics = default;
            return false;
        }
    }

    /// <summary>
    ///     Font type that renders vector fonts such as OTF and TTF fonts from a <see cref="FontResource"/>
    /// </summary>
    public sealed class VectorFont : Font
    {
        public int Size { get; }

        internal IFontInstanceHandle Handle { get; }

        public VectorFont(FontResource res, int size)
        {
            Size = size;
            Handle = IoCManager.Resolve<IFontManagerInternal>().MakeInstance(res.FontFaceHandle, size);
        }

        public override int GetAscent(float scale) => Handle.GetAscent(scale);
        public override int GetHeight(float scale) => Handle.GetHeight(scale);
        public override int GetDescent(float scale) => Handle.GetDescent(scale);
        public override int GetLineHeight(float scale) => Handle.GetLineHeight(scale);

        public override float DrawChar(DrawingHandleScreen handle, char chr, Vector2 baseline, float scale, Color color)
        {
            var metrics = Handle.GetCharMetrics(chr, scale);
            if (!metrics.HasValue)
            {
                return 0;
            }

            var texture = Handle.GetCharTexture(chr, scale);
            if (texture == null)
            {
                return metrics.Value.Advance;
            }

            baseline += new Vector2(metrics.Value.BearingX, -metrics.Value.BearingY);
            handle.DrawTexture(texture, baseline, color);
            return metrics.Value.Advance;
        }

        public override CharMetrics? GetCharMetrics(char chr, float scale)
        {
            return Handle.GetCharMetrics(chr, scale);
        }
    }

    public sealed class DummyFont : Font
    {
        public override int GetAscent(float scale) => default;
        public override int GetHeight(float scale) => default;
        public override int GetDescent(float scale) => default;
        public override int GetLineHeight(float scale) => default;

        public override float DrawChar(DrawingHandleScreen handle, char chr, Vector2 baseline, float scale, Color color)
        {
            // Nada, it's a dummy after all.
            return 0;
        }

        public override CharMetrics? GetCharMetrics(char chr, float scale)
        {
            // Nada, it's a dummy after all.
            return null;
        }
    }
}
