using System;
using System.Collections.Generic;
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
    /// </summary>
    internal struct RichTextEntry
    {
        private static readonly Color DefaultColor = new(200, 200, 200);

        private readonly MarkupTagManager _tagManager;

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
        ///     The combined text indices in the message's text tags to put line breaks.
        /// </summary>
        public ValueList<int> LineBreaks;

        //TODO: fill this dictionary by iterating over MarkupTags inside the constructor and use the index from iterating as the dictionary key
        private readonly Dictionary<int, Control> tagControls = new();

        public RichTextEntry(FormattedMessage message, MarkupTagManager tagManager)
        {
            Message = message;
            Height = 0;
            Width = 0;
            LineBreaks = default;
            _tagManager = tagManager;
        }

        /// <summary>
        ///     Recalculate line dimensions and where it has line breaks for word wrapping.
        /// </summary>
        /// <param name="defaultFont">The font being used for display.</param>
        /// <param name="maxSizeX">The maximum horizontal size of the container of this entry.</param>
        /// <param name="uiScale"></param>
        public void Update(Font defaultFont, float maxSizeX, float uiScale)
        {
            // This method is gonna suck due to complexity.
            // Bear with me here.
            // I am so deeply sorry for the person adding stuff to this in the future.

            Height = defaultFont.GetHeight(uiScale);
            LineBreaks.Clear();

            int? breakLine;
            var wordWrap = new WordWrap(maxSizeX);
            var context = new MarkupDrawingContext();
            context.Font.Push(defaultFont);
            context.Color.Push(DefaultColor);

            // Go over every node.
            // Nodes can change the markup drawing context and return additional text.
            // It's also possible for nodes to return inline controls. They get treated as one large rune. (Not yet implemented.)
            foreach (var node in Message.Nodes)
            {
                var text = ProcessNode(node, context);

                // And go over every character.
                foreach (var rune in text.EnumerateRunes())
                {
                    wordWrap.NextRune(rune, out breakLine, out var breakNewLine, out var skip);
                    CheckLineBreak(ref this, breakLine);
                    CheckLineBreak(ref this, breakNewLine);
                    if (skip)
                        continue;

                    // Uh just skip unknown characters I guess.
                    if (!context.Font.Peek().TryGetCharMetrics(rune, uiScale, out var metrics))
                        continue;

                    wordWrap.NextMetrics(metrics, out breakLine, out var abort);
                    CheckLineBreak(ref this, breakLine);
                    if (abort)
                        return;
                }
            }

            Width = wordWrap.FinalizeText(out breakLine);
            CheckLineBreak(ref this, breakLine);

            void CheckLineBreak(ref RichTextEntry src, int? line)
            {
                if (line is { } l)
                {
                    src.LineBreaks.Add(l);
                    src.Height += context.Font.Peek().GetLineHeight(uiScale);
                }
            }
        }

        public readonly void Draw(
            DrawingHandleScreen handle,
            Font defaultFont,
            UIBox2 drawBox,
            float verticalOffset,
            MarkupDrawingContext context,
            float uiScale)
        {
            context.Clear();
            context.Color.Push(DefaultColor);
            context.Font.Push(defaultFont);

            var globalBreakCounter = 0;
            var lineBreakIndex = 0;
            var baseLine = drawBox.TopLeft + new Vector2(0, defaultFont.GetAscent(uiScale) + verticalOffset);

            foreach (var node in Message.Nodes)
            {
                var text = ProcessNode(node, context);
                if (!context.Color.TryPeek(out var color) || !context.Font.TryPeek(out var font))
                {
                    color = DefaultColor;
                    font = defaultFont;
                }

                foreach (var rune in text.EnumerateRunes())
                {
                    if (lineBreakIndex < LineBreaks.Count &&
                        LineBreaks[lineBreakIndex] == globalBreakCounter)
                    {
                        baseLine = new Vector2(drawBox.Left, baseLine.Y + font.GetLineHeight(uiScale));
                        lineBreakIndex += 1;
                    }

                    var advance = defaultFont.DrawChar(handle, rune, baseLine, uiScale, color);
                    baseLine += new Vector2(advance, 0);

                    globalBreakCounter += 1;
                }
            }
        }

        private readonly string ProcessNode(MarkupNode node, MarkupDrawingContext context)
        {
            // If a nodes name is null it's a text node.
            if (node.Name == null)
                return node.Value.StringValue ?? "";

            //Skip the node if there is no markup tag for it.
            if (!_tagManager.TryGetMarkupTag(node.Name, out var tag))
                return "";

            if (!node.Closing)
            {
                tag.PushDrawContext(node, context);
                return tag.TextBefore(node);
            }

            tag.PopDrawContext(node, context);
            return tag.TextAfter(node);
        }
    }
}
