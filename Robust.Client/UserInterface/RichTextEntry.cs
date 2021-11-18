using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface
{
    /// <summary>
    ///     Used by <see cref="OutputPanel"/> and <see cref="RichTextLabel"/> to handle rich text layout.
    /// </summary>
    internal struct RichTextEntry
    {
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
        public readonly List<int> LineBreaks;

        public RichTextEntry(FormattedMessage message)
        {
            Message = message;
            Height = 0;
            Width = 0;
            LineBreaks = new List<int>();
        }

        /// <summary>
        ///     Recalculate line dimensions and where it has line breaks for word wrapping.
        /// </summary>
        /// <param name="font">The font being used for display.</param>
        /// <param name="maxSizeX">The maximum horizontal size of the container of this entry.</param>
        /// <param name="uiScale"></param>
        public void Update(Font font, float maxSizeX, float uiScale)
        {
            #if false
            // This method is gonna suck due to complexity.
            // Bear with me here.
            // I am so deeply sorry for the person adding stuff to this in the future.
            Height = font.GetHeight(uiScale);
            LineBreaks.Clear();

            var maxUsedWidth = 0f;
            // Index we put into the LineBreaks list when a line break should occur.
            var breakIndexCounter = 0;
            // If the CURRENT processing word ends up too long, this is the index to put a line break.
            (int index, float lineSize)? wordStartBreakIndex = null;
            // Word size in pixels.
            var wordSizePixels = 0;
            // The horizontal position of the text cursor.
            var posX = 0;
            var lastRune = new Rune('A');
            // If a word is larger than maxSizeX, we split it.
            // We need to keep track of some data to split it into two words.
            (int breakIndex, int wordSizePixels)? forceSplitData = null;
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
                    breakIndexCounter += 1;

                    if (IsWordBoundary(lastRune, rune) || rune == new Rune('\n'))
                    {
                        // Word boundary means we know where the word ends.
                        if (posX > maxSizeX && lastRune != new Rune(' '))
                        {
                            DebugTools.Assert(wordStartBreakIndex.HasValue,
                                "wordStartBreakIndex can only be null if the word begins at a new line, in which case this branch shouldn't be reached as the word would be split due to being longer than a single line.");
                            // We ran into a word boundary and the word is too big to fit the previous line.
                            // So we insert the line break BEFORE the last word.
                            LineBreaks.Add(wordStartBreakIndex!.Value.index);
                            Height += font.GetLineHeight(uiScale);
                            maxUsedWidth = Math.Max(maxUsedWidth, wordStartBreakIndex.Value.lineSize);
                            posX = wordSizePixels;
                        }

                        // Start a new word since we hit a word boundary.
                        //wordSize = 0;
                        wordSizePixels = 0;
                        wordStartBreakIndex = (breakIndexCounter, posX);
                        forceSplitData = null;

                        // Just manually handle newlines.
                        if (rune == new Rune('\n'))
                        {
                            LineBreaks.Add(breakIndexCounter);
                            Height += font.GetLineHeight(uiScale);
                            maxUsedWidth = Math.Max(maxUsedWidth, posX);
                            posX = 0;
                            lastRune = rune;
                            wordStartBreakIndex = null;
                            continue;
                        }
                    }

                    // Uh just skip unknown characters I guess.
                    if (!font.TryGetCharMetrics(rune, uiScale, out var metrics))
                    {
                        lastRune = rune;
                        continue;
                    }

                    // Increase word size and such with the current character.
                    var oldWordSizePixels = wordSizePixels;
                    wordSizePixels += metrics.Advance;
                    // TODO: Theoretically, does it make sense to break after the glyph's width instead of its advance?
                    //   It might result in some more tight packing but I doubt it'd be noticeable.
                    //   Also definitely even more complex to implement.
                    posX += metrics.Advance;

                    if (posX > maxSizeX)
                    {
                        if (!forceSplitData.HasValue)
                        {
                            forceSplitData = (breakIndexCounter, oldWordSizePixels);
                        }

                        // Oh hey we get to break a word that doesn't fit on a single line.
                        if (wordSizePixels > maxSizeX)
                        {
                            var (breakIndex, splitWordSize) = forceSplitData.Value;
                            if (splitWordSize == 0)
                            {
                                // Happens if there's literally not enough space for a single character so uh...
                                // Yeah just don't.
                                return;
                            }

                            // Reset forceSplitData so that we can split again if necessary.
                            forceSplitData = null;
                            LineBreaks.Add(breakIndex);
                            Height += font.GetLineHeight(uiScale);
                            wordSizePixels -= splitWordSize;
                            wordStartBreakIndex = null;
                            maxUsedWidth = Math.Max(maxUsedWidth, maxSizeX);
                            posX = wordSizePixels;
                        }
                    }

                    lastRune = rune;
                }
            }

            // This needs to happen because word wrapping doesn't get checked for the last word.
            if (posX > maxSizeX)
            {
                if (!wordStartBreakIndex.HasValue)
                {
                    Logger.Error(
                        "Assert fail inside RichTextEntry.Update, " +
                        "wordStartBreakIndex is null on method end w/ word wrap required. " +
                        "Dumping relevant stuff. Send this to PJB.");
                    Logger.Error($"Message: {Message}");
                    Logger.Error($"maxSizeX: {maxSizeX}");
                    Logger.Error($"maxUsedWidth: {maxUsedWidth}");
                    Logger.Error($"breakIndexCounter: {breakIndexCounter}");
                    Logger.Error("wordStartBreakIndex: null (duh)");
                    Logger.Error($"wordSizePixels: {wordSizePixels}");
                    Logger.Error($"posX: {posX}");
                    Logger.Error($"lastChar: {lastRune}");
                    Logger.Error($"forceSplitData: {forceSplitData}");
                    Logger.Error($"LineBreaks: {string.Join(", ", LineBreaks)}");

                    throw new Exception(
                        "wordStartBreakIndex can only be null if the word begins at a new line," +
                        "in which case this branch shouldn't be reached as" +
                        "the word would be split due to being longer than a single line.");
                }

                LineBreaks.Add(wordStartBreakIndex!.Value.index);
                Height += font.GetLineHeight(uiScale);
                maxUsedWidth = Math.Max(maxUsedWidth, wordStartBreakIndex.Value.lineSize);
            }
            else
            {
                maxUsedWidth = Math.Max(maxUsedWidth, posX);
            }

            Width = (int) maxUsedWidth;
            #endif
        }

        public void Draw(
            DrawingHandleScreen handle,
            Font font,
            UIBox2 drawBox,
            float verticalOffset,
            float uiScale)
        {
            #if false
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
                        var popped = formatStack.Pop();
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
                            globalBreakCounter += 1;

                            if (lineBreakIndex < LineBreaks.Count &&
                                LineBreaks[lineBreakIndex] == globalBreakCounter)
                            {
                                baseLine = new Vector2(drawBox.Left, baseLine.Y + font.GetLineHeight(uiScale));
                                lineBreakIndex += 1;
                            }

                            var advance = font.DrawChar(handle, rune, baseLine, uiScale, currentColorTag.Color);
                            baseLine += new Vector2(advance, 0);
                        }

                        break;
                    }
                }
            }
            #endif
        }

        [Pure]
        private static bool IsWordBoundary(Rune a, Rune b)
        {
            return a == new Rune(' ') || b == new Rune(' ') || a == new Rune('-') || b == new Rune('-');
        }
    }
}
