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
        /// <param name="w">The width the word (i.e. the sum of all its <c>Advance</c>'s).</param>
        /// <param name="h">The height of the tallest character's <c>BearingY</c>.</param>
        /// <param name="ln">The line number that the word is assigned to.</param>
        /// <param name="spw">The width allocated to this word.</param>
        /// <param name="ot">The detected type of the offset.</param>
        /// <param name="rw">The width of each rune.</param>
        public record struct Offset
        {
            public int section;
            public int charOffs;
            public int length;
            public int x;
            public int y;
            public int h;
            public int w;
            public int ln;
            public int spw;
            public OffsetType ot;
            public int[] rw;

            /// <summary>
            /// Draws a little arrow to show the extend of the <see cref="Offset"/>.
            /// Will need to be modified if the <see cref="Section"/> is long enough to wrap.
            /// </summary>
            public string ToArrows()
            {
                var sb = new StringBuilder()
                    .Append(' ', charOffs)
                    .Append('^');

                if (length > 1)
                    sb.Append('-', length - 2);

                if (length > 0)
                    sb.Append('^');

                return sb.ToString();
            }
        }

        public enum OffsetType : byte
        {
            /// <summary>
            /// Plain text that should be rendered contiguously if possible, or split if not.
            /// </summary>
            Normal,

            /// <summary>
            /// A boundary upon which other <see cref="Offset"/>s can optionally be split.
            /// </summary>
            Space,

            /// <summary>
            /// A location that MUST cause the layout system to render following <see cref="Offset"/>s on a new line.
            /// </summary>
            /// <remarks>
            /// The text layout system expects the contents to be EXACTLY one <c>\n</c>.
            /// The behavior <see cref="Offset"/>s with <see cref="Offset.length"/> &gt; <c>1</c>, or with other contents is undefined.
            /// </remarks>
            LineBreak,
        }

        public static OffsetType Classify(Rune r)
        {
            if (r == (Rune) '\n')
                return OffsetType.LineBreak;
            else if (Rune.IsSeparator(r))
                return OffsetType.Space;

            return OffsetType.Normal;
        }

        /// <seealso cref="Layout(ISectionable, List{Offset}, int, IFontLibrary, float, int, FontClass?, LayoutOptions)"/>
        public static ImmutableArray<Offset> Layout(
                ISectionable text,
                int w,
                IFontLibrary fonts,
                float scale = 1.0f,
                int lineSpacing = 0,
                FontClass? fclass = default,
                LayoutOptions options = LayoutOptions.Default
        ) => Layout(
            text,
            Split(text, fonts, scale, fclass, options),
            w,
            fonts,
            scale,
            lineSpacing,
            fclass,
            options
        );


        /// <summary>
        /// Take <see cref="Offset"/>s from <see cref="Split(ISectionable, IFontLibrary, float, FontClass?, LayoutOptions)"/> and
        /// lay them out within the constraints of the area given.
        /// </summary>
        ///
        /// <remarks>
        /// The algorithm is basically ripped from CSS Flexbox.
        ///
        /// <list type="number">
        /// <item><description>Add up all the space each word takes</description></item>
        /// <item><description>Subtract that from the line width (w)</description></item>
        /// <item><description>Save that as the free space (fs)</description></item>
        /// <item><description>Add up each gap's priority value (Σpri)</description></item>
        /// <item><description>Assign each gap a final priority (fp) of ((priMax - pri) / Σpri)</description></item>
        /// <item><description>That space has (fp*fs) pixels.</description></item>
        /// </list>
        /// </remarks>
        public static ImmutableArray<Offset> Layout(
                ISectionable src,
                List<Offset> text,
                int w,
                IFontLibrary fonts,
                float scale = 1.0f,
                int lineSpacing = 0,
                FontClass? fclass = default,
                LayoutOptions options = LayoutOptions.Default
        )
        {
            // how about no
            if (w == 0)
                return ImmutableArray<Offset>.Empty;

            var lw = new WorkQueue<(
                    List<Offset> wds,
                    List<int> gaps,
                    int lnrem,
                    int sptot,
                    int maxPri,
                    int tPri,
                    int lnh
            )>(
                blank: () => new () { lnrem = w, maxPri = 1, wds = new List<Offset>(), gaps = new List<int>() }
            );

            var lastAlign = TextAlign.Left;

            // Since we edit this one, we need to make a copy.
            var wdq = text.ShallowClone();

            // forced newline, disables skipping leading spaces
            var fnl = false;

            // Calculate line boundaries
            for (var i = 0; i < wdq.Count; i++)
            {
restart:
                var wd = wdq[i];
                var sec = src[wd.section];
                var hz = sec.Alignment.Horizontal();
                (int gW, int adv) = TransitionWeights(lastAlign, hz);

                if (!fnl && wd.ot == OffsetType.Space && lw.Work.wds.Count == 0)
                    continue;

                fnl=false;

                if (wd.ot == OffsetType.LineBreak)
                {
                    lw.Flush();
                    fnl=true;
                }
                else if (lw.Work.lnrem < wd.w)
                {
                    // We won't split if we are asked not to, or if the word can fit on one line.
                    if (!options.HasFlag(LayoutOptions.NoWordSplit) && wd.w > w)
                    {
                        var sbo = 0; // section byte offset
                        var j = 0; // just a rune counter (to index wd.rw)
                        var swdw = 0; // sub-word width

                        foreach (var r in src[wd.section]
                                .Content.Substring(wd.charOffs, wd.length)
                                .EnumerateRunes())
                        {
                            if (swdw + wd.rw[j] > lw.Work.lnrem && j > 0)
                            {
                                // the half that stays on the current line
                                var left = wd with {
                                    length=sbo,
                                    w=swdw,
                                    rw=wd.rw[0..j]
                                };

                                // the half that gets moved down
                                var right = wd with {
                                    charOffs=wd.charOffs+left.length,
                                    length=wd.length-left.length,
                                    w=wd.w-left.w,
                                    rw=wd.rw[(j-1)..^1],
                                };

                                // replace this word with the first half of itself
                                wdq[i] = left;

                                // and add the new half to the queue
                                wdq.Insert(i+1, right);

                                // reprocess from the start
                                goto restart;
                            }

                            // Advance our various counters
                            sbo += r.Utf16SequenceLength;
                            swdw += wd.rw[j];
                            j++;
                        }
                    }
                    else
                    {
                        lw.Flush();
                        if (wd.ot == OffsetType.Space)
                            continue;
                    }
                }

                lastAlign = hz;

                lw.Work.gaps.Add(gW+lw.Work.maxPri);
                lw.Work.tPri += gW+lw.Work.maxPri;
                lw.Work.maxPri += adv;
                lw.Work.lnh = Math.Max(lw.Work.lnh, wd.h);
                lw.Work.sptot += wd.spw;
                lw.Work.lnrem -= wd.w + wd.spw;
                lw.Work.wds.Add(wd);
            }
            lw.Flush(true);

            var flib = fonts.StartFont(fclass);
            int py = flib.Current.GetAscent(scale);
            int lnnum = 0;
            foreach (var (ln, gaps, lnrem, sptot, maxPri, tPri, lnh) in lw.Done)
            {
                int px=0, maxlh=0;

                var spDist = new int[gaps.Count];
                for (int i = 0; i < gaps.Count; i++)
                    spDist[i] = (int) (((float) gaps[i] / (float) tPri) * (float) sptot);

                int prevAsc=0, prevDesc=0;
                for (int i = 0; i < ln.Count; i++)
                {
                    var ss = src[ln[i].section];
                    var sf = flib.Update(ss.Style, ss.Size);
                    var asc = sf.GetAscent(scale);
                    var desc = sf.GetDescent(scale);
                    maxlh = Math.Max(maxlh, sf.GetAscent(scale));

                    if (i - 1 > 0 && i - 1 < spDist.Length)
                    {
                        px += spDist[i - 1] / 2;
                    }

                    ln[i] = ln[i] with {
                        x = px,
                        y = py + ss.Alignment.Vertical() switch {
                            TextAlign.Baseline => 0,
                            TextAlign.Bottom => -(desc - prevDesc), // Scoot it up by the descent
                            TextAlign.Top => (asc - prevAsc),
                            TextAlign.Subscript => -ln[i].h / 8,  // Technically these should be derived from the font data,
                            TextAlign.Superscript => ln[i].h / 4, // but I'm not gonna bother figuring out how to pull it from them.
                            _ => 0,
                        },
                        ln = lnnum,
                    };

                    if (i < spDist.Length)
                    {
                        px += spDist[i] / 2 + ln[i].w;
                    }

                    prevAsc = asc;
                    prevDesc = desc;
                }
                py += options.HasFlag(LayoutOptions.UseRenderTop) ? lnh : (lineSpacing + maxlh);

                lnnum++;
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

        /// <summary>
        /// Create a list of words broken based on their boundaries.
        /// </summary>
        /// <remarks>
        /// Users are encouraged to reuse this for as long as it accurately reflects
        /// the content they're trying to display.
        /// </remarks>
        public static List<Offset> Split(
                ISectionable text,
                IFontLibrary fonts,
                float scale,
                FontClass? fclass,
                LayoutOptions options = LayoutOptions.Default
        )
        {
            var nofb = options.HasFlag(LayoutOptions.NoFallback);

            // ugly stateful stuff
            var s=0; // what section we're in
            var lsbo=0; // the Last Section Byte Offset
            var sbo=0; // the current Section Byte Offset
            var runew = new int[0]; // the width of the runes in this Offset

            var wq = new WorkQueue<Offset>(
                    conv: w =>
                    {
                        // we do a little cheating and calculate the length here
                        var len = sbo-lsbo;
                        lsbo = sbo;
                        var o = w with { length=len, rw=runew[w.charOffs..(w.charOffs+len)] };
                        return o;
                    },
                    blank: default,
                    check: w => w.ot != OffsetType.Normal || sbo > lsbo, // if these aren't true, we don't need to bother committing anything
                    postcreate: w => w with { section=s, charOffs=sbo } // add The Stuff
            );

            var flib = fonts.StartFont(fclass);
            for (s = 0; s < text.Length; s++)
            {
                var sec = text[s];

                if (sec.Meta != default)
                    throw new Exception("Section with unknown or unimplemented Meta flag");

                // reset everything
                runew = new int[sec.Content.EnumerateRunes().Count()];
                lsbo = 0;
                sbo = 0;
                var fnt = flib.Update(sec.Style, sec.Size);
                wq.Reset();

                var runec=0;
                foreach (var r in sec.Content.EnumerateRunes())
                {
                    OffsetType cr = Classify(r);
                    if (wq.Work.ot != cr || cr == OffsetType.LineBreak)
                    {
                        wq.Flush();
                        wq.Work.ot = cr;
                        if (cr == OffsetType.LineBreak)
                                wq.Flush();
                    }

                    var cm = fnt.GetCharMetrics(r, scale, !nofb);
                    if (!cm.HasValue)
                    {
                        if (nofb)
                        {
                            runec++;
                            sbo += r.Utf16SequenceLength;
                            continue;
                        }
                        else if (fnt is DummyFont)
                            cm = new CharMetrics();
                        else
                            throw new Exception("unable to get character metrics");
                    }

                    wq.Work.h = Math.Max(wq.Work.h, cm.Value.Height);
                    runew[runec] = cm.Value.Advance;
                    wq.Work.w += cm.Value.Advance;
                    sbo += r.Utf16SequenceLength;
                    runec++;
                }

                wq.Flush(true);
            }

            return wq.Done;
        }

        /// <summary>
        /// Flags that control the operation of <see cref="Layout(ISectionable, List{Offset}, int, IFontLibrary, float, int, FontClass?, LayoutOptions)"/>
        /// </summary>
        [Flags]
        public enum LayoutOptions : byte
        {
            Default      = 0b0000_0000,

            /// <summary>Measure the actual height of runes to space lines.</summary>
            UseRenderTop = 0b0000_0001,

            /// <summary>Disables the use of the Fallback character.</summary>
            NoFallback   = 0b0000_0010,

            /// <summary>Disable splitting words that run over the line boundary.</summary>
            NoWordSplit  = 0b0000_0100,
        }

        /// <summary>
        /// An "assembly line" of sorts.
        /// <list type="number">
        /// <item><description>A <typeparamref name="TIn"/> (<see cref="WorkQueue{TIn, TOut}.Work"/>) is instantiated by the <see cref="WorkQueue{TIn, TOut}._blank"/> function.</description></item>
        /// <item><description><see cref="WorkQueue{TIn, TOut}._postcr"/> ("post-create") modifies <see cref="WorkQueue{TIn, TOut}.Work"/> if needed.</description></item>
        /// <item><description><see cref="WorkQueue{TIn, TOut}.Work"/> is then modified by some outside process.</description></item>
        /// <item><description>Once finished modifying <see cref="WorkQueue{TIn, TOut}.Work"/>, the outside process calls <see cref="WorkQueue{TIn, TOut}.Flush(bool)"/>.</description></item>
        /// <item><description><see cref="WorkQueue{TIn, TOut}._check"/> inspects <see cref="WorkQueue{TIn, TOut}.Work"/> to see if it needs to be committed.</description></item>
        /// <item><description>If so, <see cref="WorkQueue{TIn, TOut}._conv"/> converts it from <typeparamref name="TIn"/> to <typeparamref name="TOut"/></description></item>
        /// <item><description>If not, no changes are made.</description></item>
        /// <item><description>The resultant <typeparamref name="TOut"/> is added to <see cref="WorkQueue{TIn, TOut}.Done"/></description></item>
        /// <item><description><see cref="WorkQueue{TIn, TOut}._blank"/> creates a new instance of <typeparamref name="TIn"/> and the cycle repeats.</description></item>
        /// <item><description>Eventually, the external process uses the compilation <see cref="WorkQueue{TIn, TOut}.Done"/> to do something else.</description></item>
        /// </list>
        /// </summary>
        ///
        /// <remarks>
        /// "WorkQueue" is probably a misnomer. All it does is streamline a pattern I ended up using
        /// repeatedly where I'd have a list of something and a WIP, then I'd flush the WIP in to
        /// the list.
        /// </remarks>
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

            public void Reset()
            {
                Work = _blank.Invoke();
                if (_postcr is not null)
                    Work = _postcr.Invoke(Work);
            }

            /// <summary>
            /// Convert and commit <see cref="Work"/> to <see cref="Done"/> if <see cref="_check"/> returns true, or <paramref name="force"/> is true.
            /// Then, create a new instance of <typeparamref name="TIn"/> to replace <see cref="Work"/>.
            /// Finally, invoke <see cref="_postcr"/> on <see cref="Work"/>.
            /// </summary>
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

        /// <summary> A special case of <see cref="WorkQueue{TIn, TOut}"/> where both types are the same.</summary>
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
