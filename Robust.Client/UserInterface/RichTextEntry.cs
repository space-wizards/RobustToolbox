using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
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
        private static readonly FormattedMessage.TagColor TagBaseColor
            = new(new Color(200, 200, 200));

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

        public RichTextEntry(FormattedMessage message)
        {
            Message = message;
            Height = 0;
            Width = 0;
            LineBreaks = default;
        }

        /// <summary>
        ///     Recalculate line dimensions and where it has line breaks for word wrapping.
        /// </summary>
        /// <param name="font">The font being used for display.</param>
        /// <param name="maxSizeX">The maximum horizontal size of the container of this entry.</param>
        /// <param name="uiScale"></param>
        public void Update(Font font, float maxSizeX, float uiScale)
        {
            // This method is gonna suck due to complexity.
            // Bear with me here.
            // I am so deeply sorry for the person adding stuff to this in the future.

            Height = font.GetHeight(uiScale);
            LineBreaks.Clear();

            int? breakLine;
            var wordWrap = new WordWrap(maxSizeX);

            // Go over every text tag.
            // We treat multiple text tags as one continuous one.
            // So changing color inside a single word doesn't create a word break boundary.
            foreach (var tag in Message.Tags)
            {
                // For now we can ignore every entry that isn't a text tag because those are only color related.
                // For now.
                if (!(tag is FormattedMessage.TagText tagText))
                {
                    continue;
                }

                var text = tagText.Text;
                // And go over every character.
                foreach (var rune in text.EnumerateRunes())
                {
                    wordWrap.NextRune(rune, out breakLine, out var breakNewLine, out var skip);
                    CheckLineBreak(ref this, breakLine);
                    CheckLineBreak(ref this, breakNewLine);
                    if (skip)
                        continue;

                    // Uh just skip unknown characters I guess.
                    if (!font.TryGetCharMetrics(rune, uiScale, out var metrics))
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
                    src.Height += font.GetLineHeight(uiScale);
                }
            }
        }

        public readonly void Draw(
            DrawingHandleScreen handle,
            Font font,
            UIBox2 drawBox,
            float verticalOffset,
            // A stack for format tags.
            // This stack contains the format tag to RETURN TO when popped off.
            // So when a new color tag gets hit this stack gets the previous color pushed on.
            Stack<FormattedMessage.Tag> formatStack, float uiScale)
        {
            // The tag currently doing color.
            var currentColorTag = TagBaseColor;

            var globalBreakCounter = 0;
            var lineBreakIndex = 0;
            var baseLine = drawBox.TopLeft + new Vector2(0, font.GetAscent(uiScale) + verticalOffset);
            formatStack.Clear();
            foreach (var tag in Message.Tags)
            {
                switch (tag)
                {
                    case FormattedMessage.TagColor tagColor:
                        formatStack.Push(currentColorTag);
                        currentColorTag = tagColor;
                        break;
                    case FormattedMessage.TagPop _:
                        if (!formatStack.TryPop(out var popped))
                            throw new Exception($"Rich text entry has unmatched closing tag: {Message.ToMarkup()}");
                        switch (popped)
                        {
                            case FormattedMessage.TagColor tagColor:
                                currentColorTag = tagColor;
                                break;
                            default:
                                throw new InvalidOperationException();
                        }

                        break;
                    case FormattedMessage.TagText tagText:
                    {
                        var text = tagText.Text;
                        foreach (var rune in text.EnumerateRunes())
                        {
                            if (lineBreakIndex < LineBreaks.Count &&
                                LineBreaks[lineBreakIndex] == globalBreakCounter)
                            {
                                baseLine = new Vector2(drawBox.Left, baseLine.Y + font.GetLineHeight(uiScale));
                                lineBreakIndex += 1;
                            }

                            var advance = font.DrawChar(handle, rune, baseLine, uiScale, currentColorTag.Color);
                            baseLine += new Vector2(advance, 0);

                            globalBreakCounter += 1;
                        }

                        break;
                    }
                }
            }
        }
    }
}
