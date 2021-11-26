using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface
{
    public static class TextLayout
    {
        /// <summary>
        /// An Offset is a simplified instruction for rendering a text block.
        /// </summary>
        ///
        /// <remarks>
        /// Pseudocode for rendering:
        /// <code>
        ///     (int x, int y) topLeft = (10, 20);
        ///     var libn = style.FontLib.StartFont(defaultFontID, defaultFontStyle, defaultFontSize);
        ///     foreach (var r in returnedWords)
        ///     {
        ///          var section = Message.Sections[section];
        ///          var font = libn.Update(section.Style, section.Size);
        ///          font.DrawAt(
        ///              text=section.Content.Substring(charOffs, length),
        ///              x=topLeft.x + r.x,
        ///              y=topLeft.y + r.y,
        ///              color=new Color(section.Color)
        ///          )
        ///     }
        /// </code>
        /// </remarks>
        ///
        /// <param name="section">
        /// The index of the backing store (usually a <see cref="Robust.Shared.Utility.Section"/>) in the
        /// container (usually a <see cref="Robust.Shared.Utility.FormattedMessage"/>) to which the Offset belongs.
        /// </param>
        /// <param name="charOffs">The byte offset in to the <see cref="Robust.Shared.Utility.Section.Content"/> to render.</param>
        /// <param name="length">The number of bytes after <paramref name="charOffs"/> to render.</param>
        /// <param name="x">The offset from the base position's x coordinate to render this chunk of text.</param>
        /// <param name="y">The offset from the base position's y coordinate to render this chunk of text.</param>
        public class Offset
        {
            public Offset()
            {
            }

            public Offset(Offset o)
            {
                section = o.section;
                charOffs = o.charOffs;
                length = o.length;
                x = o.x;
                y = o.y;
            }

            public int section;
            public int charOffs;
            public int length;
            public int x;
            public int y;
        }

        public enum WordType : byte
        {
            Normal,
            Space,
            LineBreak,
        }

        public sealed class Word : Offset
        {
            public Word()
            {
            }

            public Word(Word o) : base(o)
            {
                h = o.h;
                w = o.w;
                spw = o.spw;
                wt = o.wt;
            }
            public int h;
            public int w;
            public int spw;
            public WordType wt;
        }

        public static ImmutableArray<Word> Layout(
                ISectionable text,
                int w,
                IFontLibrary fonts,
                float scale = 1.0f,
                int lineSpacing = 0,
                int wordSpacing = 0,
                int runeSpacing = 0,
                FontClass? fclass = default,
                LayoutOptions options = LayoutOptions.Default
        ) => Layout(
            text,
            Split(text, fonts, scale, wordSpacing, runeSpacing, fclass, options),
            w,
            fonts,
            scale,
            lineSpacing, wordSpacing,
            fclass,
            options
        );

        // Actually produce the layout data.
        // The algorithm is basically ripped from CSS Flexbox.
        //
        // 1. Add up all the space each word takes
        // 2. Subtract that from the line width (w)
        // 3. Save that as the free space (fs)
        // 4. Add up each gap's priority value (Σpri)
        // 5. Assign each gap a final priority (fp) of ((priMax - pri) / Σpri)
        // 6. That space has (fp*fs) pixels.
        public static ImmutableArray<Word> Layout(
                ISectionable src,
                ImmutableArray<Word> text,
                int w,
                IFontLibrary fonts,
                float scale = 1.0f,
                int lineSpacing = 0,
                int wordSpacing = 0,
                FontClass? fclass = default,
                LayoutOptions options = LayoutOptions.Default
        )
        {
            var lw = new WorkQueue<(
                    List<Word> wds,
                    List<int> gaps,
                    int lnrem,
                    int sptot,
                    int maxPri,
                    int tPri
            )>(postcreate: i => i with
            {
                wds = new List<Word>(),
                gaps = new List<int>()
            });

            var lastAlign = TextAlign.Left;

            // Calculate line boundaries
            foreach (var wd in text)
            {
                var hz = src[wd.section].Alignment.Horizontal();
                (int gW, int adv) = TransitionWeights(lastAlign, hz);
                lastAlign = hz;

                lw.Work.gaps.Add(gW+lw.Work.maxPri);
                lw.Work.tPri += gW+lw.Work.maxPri;
                lw.Work.maxPri += adv;

                if (lw.Work.lnrem < wd.w)
                {
                    lw.Flush();
                    lw.Work.lnrem = w;
                    lw.Work.maxPri = 1;
                }

                lw.Work.sptot += wd.spw;
                lw.Work.lnrem -= wd.w + wd.spw;
                lw.Work.wds.Add(wd);
            }
            lw.Flush(true);

            var flib = fonts.StartFont(fclass);
            int py=0;
            foreach ((var ln, var gaps, var lnrem, var sptot, var maxPri, var tPri) in lw.Done)
            {
                int px=0;
                int lh=0;
                var spDist = new int[gaps.Count];
                for (int i = 0; i < gaps.Count; i++)
                    spDist[i] = (int) (((float) gaps[i] / (float) tPri) * (float) sptot);

                int prevasc=0, prevdesc=0;
                for (int i = 0; i < ln.Count; i++)
                {
                    var ss = src[ln[i].section];
                    var sf = flib.Update(ss.Style, ss.Size);
                    var asc = sf.GetAscent(scale);
                    var desc = sf.GetDescent(scale);
                    px += spDist[i] + ln[i].w;
                    ln[i].x = px;
                    lh = Math.Max(lh, ln[i].h);
                    ln[i].y = src[ln[i].section].Alignment.Vertical() switch {
                        TextAlign.Baseline => 0,
                        TextAlign.Bottom => -(desc - prevdesc), // Scoot it up by the descent
                        TextAlign.Top => (asc - prevasc),
                        TextAlign.Subscript => -ln[i].h / 8,  // Technically these should be derived from the font data,
                        TextAlign.Superscript => ln[i].h / 4, // but I'm not gonna bother figuring out how to pull it from them.
                        _ => 0,
                    };
                    prevasc = asc;
                    prevdesc = desc;
                }
                py += lineSpacing + lh;
            }

            return lw.Done.SelectMany(e => e.wds).ToImmutableArray();
        }

        private static (int gapPri, int adv) TransitionWeights (TextAlign l, TextAlign r)
        {
            l = l.Horizontal();
            r = r.Horizontal();

            // Technically these could be slimmed down, but it's as much to help explain the system
            // as it is to implement it.

            // p (aka gapPri) is how high up the food chain each gap should be.
            // _LOWER_ p means more (since we do first-come first-serve).

            // a (aka adv) is how much we increment the gapPri counter, meaning how much less important
            // future alignment changes are.

            // Left alignment.
            (int p, int a) la = (l, r) switch {
                (   TextAlign.Left,     TextAlign.Left) => (0, 0), // Left alignment doesn't care about inter-word spacing
                (                _,     TextAlign.Left) => (0, 0), // or anything that comes before it,
                (   TextAlign.Left,                  _) => (1, 1), // only what comes after it.
                (                _,                  _) => (0, 0)
            };

            // Right alignment
            (int p, int a) ra = (l, r) switch {
                (  TextAlign.Right,    TextAlign.Right) => (0, 0), // Right alignment also does not care about inter-word spacing,
                (                _,    TextAlign.Right) => (1, 1), // but it does care what comes before it,
                (  TextAlign.Right,                  _) => (0, 0), // but not after.
                (                _,                  _) => (0, 0)
            };

            // Centering
            (int p, int a) ca = (l, r) switch {
                ( TextAlign.Center,   TextAlign.Center) => (0, 0), // Centering still doesn't care about inter-word spacing,
                (                _,   TextAlign.Center) => (1, 0), // but it cares about both what comes before it,
                ( TextAlign.Center,                  _) => (1, 1), // and what comes after it.
                (                _,                  _) => (0, 0)
            };

            // Justifying
            (int p, int a) ja = (l, r) switch {
                (TextAlign.Justify,  TextAlign.Justify) => (1, 0), // Justification cares about inter-word spacing.
                (                _,  TextAlign.Justify) => (0, 1), // And (sort of) what comes before it.
                (                _,                  _) => (0, 0)
            };

            return new
            (
                    la.p + ra.p + ca.p + ja.p,
                    la.a + ra.a + ca.a + ja.a
            );
        }

        // Split creates a list of words broken based on their boundaries.
        // Users are encouraged to reuse this for as long as it accurately reflects
        // the content they're trying to display.
        public static ImmutableArray<Word> Split(
                ISectionable text,
                IFontLibrary fonts,
                float scale,
                int wordSpacing,
                int runeSpacing,
                FontClass? fclass,
                LayoutOptions options = LayoutOptions.Default
        )
        {
            var nofb = options.HasFlag(LayoutOptions.NoFallback);

            var s=0;
            var lsbo=0;
            var sbo=0;
            var wq = new WorkQueue<Word>(
                    w =>
                    {
                        var len = lsbo-sbo;
                        lsbo = sbo;
                        sbo = 0;
                        return new(w) { length=len };
                    },
                    () => new Word() { section=s, charOffs=sbo },
                    w => w.length > 0
            );

            var flib = fonts.StartFont(fclass);
            for (s = 0; s < text.Length; s++)
            {
                var sec = text[s];

                #warning Meta.Localized not yet implemented
                if (sec.Meta != default)
                    throw new Exception("Text section with unknown or unimplemented Meta flag");

                sbo = 0;
                var fnt = flib.Update(sec.Style, sec.Size);

                foreach (var r in sec.Content.EnumerateRunes())
                {
                    if (r == (Rune) '\n')
                    {
                        wq.Flush();
                        wq.Work.wt = WordType.LineBreak;
                    }
                    else if (Rune.IsSeparator(r))
                    {
                        if (wq.Work.wt != WordType.Space)
                        {
                            wq.Work.w += wordSpacing;
                            wq.Flush();
                            wq.Work.wt = WordType.Space;
                        }
                    }
                    else if (wq.Work.wt != WordType.Normal)
                        wq.Flush();

                    sbo += r.Utf16SequenceLength;
                    var cm = fnt.GetCharMetrics(r, scale, !nofb);

                    if (!cm.HasValue)
                    {
                        if (nofb)
                            continue;
                        else if (fnt is DummyFont)
                            cm = new CharMetrics();
                        else
                            throw new Exception("unable to get character metrics");
                    }

                    // This may be less-than-optimal, since we're ignoring anything below the origin.
                    wq.Work.h = Math.Max(wq.Work.h, cm.Value.BearingY);
                    wq.Work.w += cm.Value.Advance;
                    if (wq.Work.wt == WordType.Normal)
                        wq.Work.spw = runeSpacing;
                }
            }

            wq.Flush(true);

            return wq.Done.ToImmutableArray();
        }

        [Flags]
        public enum LayoutOptions : byte
        {
            Default      = 0b0000_0000,

            // Measure the actual height of runes to space lines.
            UseRenderTop = 0b0000_0001,

            // NoFallback disables the use of the Fallback character.
            NoFallback   = 0b0000_0010,
        }

        // WorkQueue is probably a misnomer. All it does is streamline a pattern I ended up using
        // repeatedly where I'd have a list of something and a WIP, then I'd flush the WIP in to
        // the list.
        private class WorkQueue<TIn, TOut>
            where TIn : new()
        {
            // _blank creates a new T if _refresh says it needs to.
            private Func<TIn> _blank = () => new TIn();
            private Func<TIn, TIn>? _postcr;

            private Func<TIn, bool> _check = _ => true;

            private Func<TIn, TOut> _conv;


            public List<TOut> Done = new();
            public TIn Work;

            public WorkQueue(
                    Func<TIn, TOut> conv,
                    Func<TIn>? blank = default,
                    Func<TIn, bool>? check = default,
                    Func<TIn, TIn>? postcreate = default
            )
            {
                _conv = conv;

                if (blank is not null)
                    _blank = blank;

                if (check is not null)
                    _check = check;

                if (postcreate is not null)
                    _postcr = postcreate;

                Work = _blank.Invoke();

                if (_postcr is not null)
                    Work = _postcr.Invoke(Work);
            }

            public void Flush(bool force = false)
            {
                if (_check.Invoke(Work) || force)
                {
                    Done.Add(_conv(Work));
                    Work = _blank.Invoke();
                    if (_postcr is not null)
                        Work = _postcr.Invoke(Work);
                }
            }
        }

        private class WorkQueue<T> : WorkQueue<T, T>
            where T : new()
        {
            private static Func<T, T> __conv = i => i;
            public WorkQueue(
                    Func<T, T>? conv = default,
                    Func<T>? blank = default,
                    Func<T, bool>? check = default,
                    Func<T, T>? postcreate = default
            ) : base(conv ?? __conv, blank, check, postcreate)
            {
            }
        }
    }
}
