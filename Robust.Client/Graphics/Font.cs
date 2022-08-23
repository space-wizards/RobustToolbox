using System;
using System.Text;
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

        /// <summary>
        ///     Draw a character at a certain baseline position on screen.
        /// </summary>
        /// <param name="handle">The drawing handle to draw to.</param>
        /// <param name="rune">The Unicode code point to draw.</param>
        /// <param name="baseline">The baseline from which to draw the character.</param>
        /// <param name="scale">DPI scale factor to render the font at.</param>
        /// <param name="color">The color of the character to draw.</param>
        /// <param name="fallback">If the character is not available, render "�" instead.</param>
        /// <returns>How much to advance the cursor to draw the next character.</returns>
        public abstract float DrawChar(
            DrawingHandleScreen handle, Rune rune, Vector2 baseline, float scale,
            Color color, bool fallback=true);

        /// <summary>
        ///     Gets metrics describing the dimensions and positioning of a single glyph in the font.
        /// </summary>
        /// <param name="rune">The unicode codepoint to fetch the glyph metrics for.</param>
        /// <param name="scale">DPI scale factor to render the font at.</param>
        /// <param name="fallback">
        ///     If the character is not available, return data for "�" instead.
        ///     This can still fail if the font does not define � itself.
        /// </param>
        /// <returns>
        ///     <c>null</c> if this font does not have a glyph for the specified character,
        ///     otherwise the metrics you asked for.
        /// </returns>
        /// <seealso cref="TryGetCharMetrics"/>
        public abstract CharMetrics? GetCharMetrics(Rune rune, float scale, bool fallback=true);

        /// <summary>
        ///     Try-pattern version of <see cref="GetCharMetrics"/>.
        /// </summary>
        public bool TryGetCharMetrics(Rune rune, float scale, out CharMetrics metrics, bool fallback=true)
        {
            var maybe = GetCharMetrics(rune, scale);
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

        public override float DrawChar(DrawingHandleScreen handle, Rune rune, Vector2 baseline, float scale, Color color, bool fallback=true)
        {
            var metrics = Handle.GetCharMetrics(rune, scale);
            if (!metrics.HasValue)
            {
                if (fallback && !Rune.IsWhiteSpace(rune))
                {
                    rune = new Rune('�');
                    metrics = Handle.GetCharMetrics(rune, scale);
                    if (!metrics.HasValue)
                        return 0;
                }
                else
                    return 0;
            }

            var texture = Handle.GetCharTexture(rune, scale);
            if (texture == null)
            {
                return metrics.Value.Advance;
            }

            baseline += new Vector2(metrics.Value.BearingX, -metrics.Value.BearingY);
            handle.DrawTexture(texture, baseline, color);
            return metrics.Value.Advance;
        }

        public override CharMetrics? GetCharMetrics(Rune rune, float scale, bool fallback=true)
        {
            var metrics = Handle.GetCharMetrics(rune, scale);
            if (metrics == null && !Rune.IsWhiteSpace(rune) && fallback)
                return Handle.GetCharMetrics(new Rune('�'), scale);
            return metrics;
        }
    }

    public sealed class StackedFont : Font
    {
        // _main is the "default" font; the top of the Stack.
        public readonly Font _main;
        public readonly Font[] Stack;

        public StackedFont(params Font[] args)
        {
            if (args.Length < 1)
                throw new ArgumentException("At least one font is required");

            Stack = args;
            _main = args[0];
        }

        // All metrics methods use the default font (_main).
        // Technically these could vary between stacked fonts, but that is a case
        // that really should already be avoided for so many other reasons.
        public override int GetAscent(float scale) => _main.GetAscent(scale);
        public override int GetHeight(float scale) => _main.GetHeight(scale);
        public override int GetDescent(float scale) => _main.GetDescent(scale);
        public override int GetLineHeight(float scale) => _main.GetLineHeight(scale);

        // DrawChar just proxies to the stack, or invokes _main's fallback.
        public override float DrawChar(DrawingHandleScreen handle, Rune rune, Vector2 baseline, float scale, Color color, bool fallback=true)
        {
            foreach (var f in Stack)
            {
                var w = f.DrawChar(handle, rune, baseline, scale, color, fallback: false);
                if (w != 0f)
                    return w;
            }

            if (fallback)
                return _main.DrawChar(handle, rune, baseline, scale, color, fallback: true);

            return 0f;
        }

        public override CharMetrics? GetCharMetrics(Rune rune, float scale, bool fallback=true)
        {
            foreach (var f in Stack)
            {
                var m = f.GetCharMetrics(rune, scale, fallback: false);
                if (m != null)
                    return m;
            }

            if (!Rune.IsWhiteSpace(rune) && fallback)
                return _main.GetCharMetrics(rune, scale, fallback: true);

            return null;
        }
    }

    public sealed class DummyFont : Font
    {
        public override int GetAscent(float scale) => default;
        public override int GetHeight(float scale) => default;
        public override int GetDescent(float scale) => default;
        public override int GetLineHeight(float scale) => default;

        public override float DrawChar(DrawingHandleScreen handle, Rune rune, Vector2 baseline, float scale, Color color, bool fallback=true)
        {
            // Nada, it's a dummy after all.
            return 0;
        }

        public override CharMetrics? GetCharMetrics(Rune rune, float scale, bool fallback=true)
        {
            // Nada, it's a dummy after all.
            return null;
        }
    }
}
