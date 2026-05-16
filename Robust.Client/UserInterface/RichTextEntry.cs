using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Collections;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface
{
    /// <summary>
    ///     Used by <see cref="OutputPanel"/> and <see cref="RichTextLabel"/> to handle rich text layout.
    ///     Note that if this text is ever removed or modified without removing the owning control,
    ///     then <see cref="RemoveControls"/> should be called to ensure that any controls that were added by this
    ///     entry are also removed.
    /// </summary>
    internal struct RichTextEntry
    {
        public static readonly Type[] DefaultTags =
        [
            typeof(BoldItalicTag),
            typeof(BoldTag),
            typeof(BulletTag),
            typeof(ColorTag),
            typeof(HeadingTag),
            typeof(ItalicTag)
        ];

        private readonly Color _defaultColor;
        private readonly Type[]? _tagsAllowed;

        public readonly FormattedMessage Message;

        /// <summary>
        ///     The vertical size of this entry, in pixels.
        /// </summary>
        public int Height;

        /// <summary>
        ///     The horizontal size of this entry, in pixels.
        /// </summary>
        public int Width;

        /// <summary>
        ///     The combined layout indices in the message's text tags and inline controls to put line breaks.
        /// </summary>
        public ValueList<int> LineBreaks;

        private ValueList<LineMetrics> _lineMetrics;

        public readonly Dictionary<int, Control>? Controls;


        public RichTextEntry(
            FormattedMessage message,
            Control parent,
            MarkupTagManager tagManager,
            Color? defaultColor = null) : this(message, parent, tagManager, DefaultTags, defaultColor)
        {
            // RichTextEntry constructor but with DefaultTags
        }

        public RichTextEntry(FormattedMessage message, Control parent, MarkupTagManager tagManager, Type[]? tagsAllowed, Color? defaultColor = null)
        {
            Message = message;
            Height = 0;
            Width = 0;
            LineBreaks = default;
            _lineMetrics = default;
            _defaultColor = defaultColor ?? new(200, 200, 200);
            _tagsAllowed = tagsAllowed;
            Controls = GetControls(parent, tagManager);
        }

        private readonly Dictionary<int, Control>? GetControls(Control parent, MarkupTagManager tagManager)
        {
            Dictionary<int, Control>? tagControls = null;
            var nodeIndex = -1;

            foreach (var node in Message)
            {
                nodeIndex++;

                if (node.Name == null)
                    continue;

                if (!tagManager.TryGetMarkupTagHandler(node.Name, _tagsAllowed, out var handler) || !handler.TryCreateControl(node, out var control))
                    continue;

                // Markup tag handler instances are shared across controls. We need to ensure that the hanlder doesn't
                // store state information and return the same control for each rich text entry.
                DebugTools.Assert(handler.TryCreateControl(node, out var other) && other != control);

                parent.Children.Add(control);
                tagControls ??= new Dictionary<int, Control>();
                tagControls.Add(nodeIndex, control);
            }

            return tagControls;
        }

        // TODO RICH TEXT
        // Somehow ensure that this **has** to be called when removing rich text from some control.
        /// <summary>
        /// Remove all owned controls from their parents.
        /// </summary>
        public readonly void RemoveControls()
        {
            if (Controls == null)
                return;

            foreach (var ctrl in Controls.Values)
            {
                ctrl.Orphan();
            }
        }

        /// <summary>
        ///     Recalculate line dimensions and where it has line breaks for word wrapping.
        /// </summary>
        /// <param name="defaultFont">The font being used for display.</param>
        /// <param name="maxSizeX">The maximum horizontal size of the container of this entry.</param>
        /// <param name="uiScale"></param>
        /// <param name="lineHeightScale"></param>
        public RichTextEntry Update(MarkupTagManager tagManager, Font defaultFont, float maxSizeX, float uiScale, float lineHeightScale = 1)
        {
            // This method is gonna suck due to complexity.
            // Bear with me here.
            // I am so deeply sorry for the person adding stuff to this in the future.

            Height = 0;
            Width = 0;
            LineBreaks.Clear();
            _lineMetrics.Clear();

            int? breakLine;
            var wordWrap = new WordWrap(maxSizeX);
            ValueList<LayoutItemMetrics> itemMetrics = default;
            var context = new MarkupDrawingContext();
            context.Font.Push(defaultFont);
            context.Color.Push(_defaultColor);

            // Go over every node.
            // Nodes can change the markup drawing context and return additional text.
            // It's also possible for nodes to return inline controls. They get treated as one large rune.
            var nodeIndex = -1;
            foreach (var node in Message)
            {
                nodeIndex++;
                var text = ProcessNode(tagManager, node, context);

                if (!context.Font.TryPeek(out var font))
                    font = defaultFont;

                // And go over every character.
                foreach (var rune in text.EnumerateRunes())
                {
                    wordWrap.NextRune(rune, out breakLine, out var breakNewLine, out var skip);
                    AddLineBreak(ref this, breakLine);
                    AddLineBreak(ref this, breakNewLine);

                    if (skip)
                    {
                        itemMetrics.Add(default);
                        continue;
                    }

                    // Uh just skip unknown characters I guess.
                    if (!font.TryGetCharMetrics(rune, uiScale, out var metrics))
                    {
                        itemMetrics.Add(default);
                        continue;
                    }

                    if (ProcessMetric(ref this, metrics, out breakLine))
                        return this;

                    itemMetrics.Add(LayoutItemMetrics.ForFont(font, uiScale, lineHeightScale));
                }

                if (Controls == null || !Controls.TryGetValue(nodeIndex, out var control))
                    continue;

                control.Measure(new Vector2(maxSizeX, float.PositiveInfinity));

                var desiredSize = control.DesiredPixelSize;
                var controlMetrics = new CharMetrics(
                    0, 0,
                    desiredSize.X,
                    desiredSize.X,
                    desiredSize.Y);

                wordWrap.NextObject(controlMetrics, out breakLine, out var abort);
                AddLineBreak(ref this, breakLine);
                if (abort)
                    return this;

                itemMetrics.Add(LayoutItemMetrics.ForControl(desiredSize.Y));
            }

            Width = wordWrap.FinalizeText(out breakLine);
            AddLineBreak(ref this, breakLine);
            CalculateLineMetrics(ref this, itemMetrics, defaultFont, uiScale, lineHeightScale);

            return this;

            bool ProcessMetric(ref RichTextEntry src, CharMetrics metrics, out int? outBreakLine)
            {
                wordWrap.NextMetrics(metrics, out breakLine, out var abort);
                AddLineBreak(ref src, breakLine);
                outBreakLine = breakLine;
                return abort;
            }

            static void AddLineBreak(ref RichTextEntry src, int? line)
            {
                if (line is not { } l)
                    return;

                if (src.LineBreaks.Count > 0 && src.LineBreaks[src.LineBreaks.Count - 1] == l)
                    return;

                src.LineBreaks.Add(l);
            }

            static void CalculateLineMetrics(
                ref RichTextEntry src,
                ValueList<LayoutItemMetrics> items,
                Font defaultFont,
                float uiScale,
                float lineHeightScale)
            {
                if (items.Count == 0 && src.LineBreaks.Count == 0)
                    return;

                var lineStart = 0;
                for (var i = 0; i < src.LineBreaks.Count; i++)
                {
                    var lineEnd = Math.Clamp(src.LineBreaks[i], lineStart, items.Count);
                    AddLineMetrics(ref src, items, lineStart, lineEnd, false, defaultFont, uiScale, lineHeightScale);
                    lineStart = lineEnd;
                }

                AddLineMetrics(ref src, items, lineStart, items.Count, true, defaultFont, uiScale, lineHeightScale);
            }

            static void AddLineMetrics(
                ref RichTextEntry src,
                ValueList<LayoutItemMetrics> items,
                int start,
                int end,
                bool finalLine,
                Font defaultFont,
                float uiScale,
                float lineHeightScale)
            {
                var contentHeight = 0;
                var advance = 0;
                var ascent = 0;
                var hasContent = false;
                var hasFont = false;

                for (var i = start; i < end; i++)
                {
                    ref var item = ref items[i];
                    if (!item.HasContent)
                        continue;

                    hasContent = true;
                    contentHeight = Math.Max(contentHeight, item.ContentHeight);
                    advance = Math.Max(advance, item.Advance);

                    if (!item.HasFont)
                        continue;

                    hasFont = true;
                    ascent = Math.Max(ascent, item.Ascent);
                }

                if (!hasContent)
                {
                    contentHeight = defaultFont.GetHeight(uiScale);
                    advance = GetLineHeight(defaultFont, uiScale, lineHeightScale);
                }

                if (!hasFont)
                    ascent = defaultFont.GetAscent(uiScale);

                src._lineMetrics.Add(new LineMetrics(contentHeight, advance, ascent));
                src.Height += finalLine ? contentHeight : advance;
            }
        }

        internal readonly void HideControls()
        {
            if (Controls == null)
                return;

            foreach (var control in Controls.Values)
            {
                control.Visible = false;
            }
        }

        public readonly void Draw(
            MarkupTagManager tagManager,
            DrawingHandleBase handle,
            Font defaultFont,
            UIBox2 drawBox,
            float verticalOffset,
            MarkupDrawingContext context,
            float uiScale,
            float lineHeightScale = 1)
        {
            context.Clear();
            context.Color.Push(_defaultColor);
            context.Font.Push(defaultFont);

            var globalBreakCounter = 0;
            var lineBreakIndex = 0;
            var lineIndex = 0;
            var lineTop = drawBox.Top + verticalOffset;
            var baseLine = new Vector2(drawBox.Left, lineTop + GetLineAscent(defaultFont, uiScale, lineIndex));

            var spaceRune = new Rune(' ');

            var nodeIndex = -1;
            foreach (var node in Message)
            {
                nodeIndex++;
                var text = ProcessNode(tagManager, node, context);
                if (!context.Color.TryPeek(out var color) || !context.Font.TryPeek(out var font))
                {
                    color = _defaultColor;
                    font = defaultFont;
                }

                foreach (var rune in text.EnumerateRunes())
                {
                    bool skipSpaceBaseline = false;

                    if (lineBreakIndex < LineBreaks.Count &&
                        LineBreaks[lineBreakIndex] == globalBreakCounter)
                    {
                        lineTop += GetLineAdvance(defaultFont, uiScale, lineHeightScale, lineIndex);
                        lineIndex += 1;
                        baseLine = new Vector2(drawBox.Left, lineTop + GetLineAscent(defaultFont, uiScale, lineIndex));
                        lineBreakIndex += 1;

                        // The baseline calc is kind of messed up, the newline is After the space but the space is being drawn after doing the newline
                        // Which means if this metric Ends on a space, the next metric will use the wrong baseline when it starts, for some reason ..
                        if (rune == spaceRune)
                            skipSpaceBaseline = true;
                    }

                    var advance = font.DrawChar(handle, rune, baseLine, uiScale, color);

                    if (!skipSpaceBaseline)
                        baseLine += new Vector2(advance, 0);

                    globalBreakCounter += 1;
                }

                if (Controls == null || !Controls.TryGetValue(nodeIndex, out var control))
                    continue;

                if (lineBreakIndex < LineBreaks.Count &&
                    LineBreaks[lineBreakIndex] == globalBreakCounter)
                {
                    lineTop += GetLineAdvance(defaultFont, uiScale, lineHeightScale, lineIndex);
                    lineIndex += 1;
                    baseLine = new Vector2(drawBox.Left, lineTop + GetLineAscent(defaultFont, uiScale, lineIndex));
                    lineBreakIndex += 1;
                }

                // Controls may have been previously hidden via HideControls due to being "out-of frame".
                // If this ever gets replaced with RectClipContents / scissor box testing, this can be removed.
                control.Visible = true;

                var invertedScale = 1f / uiScale;
                control.Measure(new Vector2(Width, Height));
                control.Arrange(UIBox2.FromDimensions(
                    baseLine.X * invertedScale,
                    lineTop * invertedScale,
                    control.DesiredSize.X,
                    control.DesiredSize.Y
                ));
                var advanceX = control.DesiredPixelSize.X;
                baseLine += new Vector2(advanceX, 0);
                globalBreakCounter += 1;
            }
        }

        private readonly string ProcessNode(MarkupTagManager tagManager, MarkupNode node, MarkupDrawingContext context)
        {
            // If a nodes name is null it's a text node.
            if (node.Name == null)
                return node.Value.StringValue ?? "";

            //Skip the node if there is no markup tag for it.
            if (!tagManager.TryGetMarkupTagHandler(node.Name, _tagsAllowed, out var tag))
                return "";

            if (!node.Closing)
            {
                tag.PushDrawContext(node, context);
                return tag.TextBefore(node);
            }

            tag.PopDrawContext(node, context);
            return tag.TextAfter(node);
        }

        private static int GetLineHeight(Font font, float uiScale, float lineHeightScale)
        {
            var height = font.GetLineHeight(uiScale);
            return (int)(height * lineHeightScale);
        }

        private readonly int GetLineAdvance(Font defaultFont, float uiScale, float lineHeightScale, int lineIndex)
        {
            if (lineIndex < _lineMetrics.Count)
                return _lineMetrics[lineIndex].Advance;

            return GetLineHeight(defaultFont, uiScale, lineHeightScale);
        }

        private readonly int GetLineAscent(Font defaultFont, float uiScale, int lineIndex)
        {
            if (lineIndex < _lineMetrics.Count)
                return _lineMetrics[lineIndex].Ascent;

            return defaultFont.GetAscent(uiScale);
        }

        private readonly struct LayoutItemMetrics
        {
            public readonly int ContentHeight;
            public readonly int Advance;
            public readonly int Ascent;
            public readonly bool HasContent;
            public readonly bool HasFont;

            private LayoutItemMetrics(int contentHeight, int advance, int ascent, bool hasFont)
            {
                ContentHeight = contentHeight;
                Advance = advance;
                Ascent = ascent;
                HasContent = true;
                HasFont = hasFont;
            }

            public static LayoutItemMetrics ForFont(Font font, float uiScale, float lineHeightScale)
            {
                return new LayoutItemMetrics(
                    font.GetHeight(uiScale),
                    GetLineHeight(font, uiScale, lineHeightScale),
                    font.GetAscent(uiScale),
                    true);
            }

            public static LayoutItemMetrics ForControl(int height)
            {
                return new LayoutItemMetrics(height, height, 0, false);
            }
        }

        private readonly struct LineMetrics
        {
            public readonly int ContentHeight;
            public readonly int Advance;
            public readonly int Ascent;

            public LineMetrics(int contentHeight, int advance, int ascent)
            {
                ContentHeight = contentHeight;
                Advance = advance;
                Ascent = ascent;
            }
        }
    }
}
