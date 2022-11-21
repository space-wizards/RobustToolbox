using System;
using System.Diagnostics.Contracts;
using System.Text;
using Robust.Client.Graphics;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface;

/// <summary>
/// Helper utility struct for word-wrapping calculations.
/// </summary>
internal struct WordWrap
{
    private readonly float _maxSizeX;

    public float MaxUsedWidth;
    // Index we put into the LineBreaks list when a line break should occur.
    public int BreakIndexCounter;
    public int NextBreakIndexCounter;
    // If the CURRENT processing word ends up too long, this is the index to put a line break.
    public (int index, float lineSize)? WordStartBreakIndex;
    // Word size in pixels.
    public int WordSizePixels;
    // The horizontal position of the text cursor.
    public int PosX;
    public Rune LastRune;
    // If a word is larger than maxSizeX, we split it.
    // We need to keep track of some data to split it into two words.
    public (int breakIndex, int wordSizePixels)? ForceSplitData = null;

    public WordWrap(float maxSizeX)
    {
        this = default;
        _maxSizeX = maxSizeX;
        LastRune = new Rune('A');
    }

    public void NextRune(Rune rune, out int? breakLine, out int? breakNewLine, out bool skip)
    {
        BreakIndexCounter = NextBreakIndexCounter;
        NextBreakIndexCounter += rune.Utf16SequenceLength;

        breakLine = null;
        breakNewLine = null;
        skip = false;

        if (IsWordBoundary(LastRune, rune) || rune == new Rune('\n'))
        {
            // Word boundary means we know where the word ends.
            if (PosX > _maxSizeX && LastRune != new Rune(' '))
            {
                DebugTools.Assert(WordStartBreakIndex.HasValue,
                    "wordStartBreakIndex can only be null if the word begins at a new line, in which case this branch shouldn't be reached as the word would be split due to being longer than a single line.");
                // We ran into a word boundary and the word is too big to fit the previous line.
                // So we insert the line break BEFORE the last word.
                breakLine = WordStartBreakIndex!.Value.index;
                MaxUsedWidth = Math.Max(MaxUsedWidth, WordStartBreakIndex.Value.lineSize);
                PosX = WordSizePixels;
            }

            // Start a new word since we hit a word boundary.
            //wordSize = 0;
            WordSizePixels = 0;
            WordStartBreakIndex = (BreakIndexCounter, PosX);
            ForceSplitData = null;

            // Just manually handle newlines.
            if (rune == new Rune('\n'))
            {
                MaxUsedWidth = Math.Max(MaxUsedWidth, PosX);
                PosX = 0;
                WordStartBreakIndex = null;
                skip = true;
                breakNewLine = BreakIndexCounter;
            }
        }

        LastRune = rune;
    }

    public void NextMetrics(in CharMetrics metrics, out int? breakLine, out bool abort)
    {
        abort = false;
        breakLine = null;

        // Increase word size and such with the current character.
        var oldWordSizePixels = WordSizePixels;
        WordSizePixels += metrics.Advance;
        // TODO: Theoretically, does it make sense to break after the glyph's width instead of its advance?
        //   It might result in some more tight packing but I doubt it'd be noticeable.
        //   Also definitely even more complex to implement.
        PosX += metrics.Advance;

        if (PosX <= _maxSizeX)
            return;

        if (!ForceSplitData.HasValue)
        {
            ForceSplitData = (BreakIndexCounter, oldWordSizePixels);
        }

        // Oh hey we get to break a word that doesn't fit on a single line.
        if (WordSizePixels > _maxSizeX)
        {
            var (breakIndex, splitWordSize) = ForceSplitData.Value;
            if (splitWordSize == 0)
            {
                // Happens if there's literally not enough space for a single character so uh...
                // Yeah just don't.
                abort = true;
                return;
            }

            // Reset forceSplitData so that we can split again if necessary.
            ForceSplitData = null;
            breakLine = breakIndex;
            WordSizePixels -= splitWordSize;
            WordStartBreakIndex = null;
            MaxUsedWidth = Math.Max(MaxUsedWidth, _maxSizeX);
            PosX = WordSizePixels;
        }
    }

    public int FinalizeText(out int? breakLine)
    {
        // This needs to happen because word wrapping doesn't get checked for the last word.
        if (PosX > _maxSizeX)
        {
            if (!WordStartBreakIndex.HasValue)
            {
                Logger.Error(
                    "Assert fail inside RichTextEntry.Update, " +
                    "wordStartBreakIndex is null on method end w/ word wrap required. " +
                    "Dumping relevant stuff. Send this to PJB.");
                // Logger.Error($"Message: {Message}");
                Logger.Error($"maxSizeX: {_maxSizeX}");
                Logger.Error($"maxUsedWidth: {MaxUsedWidth}");
                Logger.Error($"breakIndexCounter: {BreakIndexCounter}");
                Logger.Error("wordStartBreakIndex: null (duh)");
                Logger.Error($"wordSizePixels: {WordSizePixels}");
                Logger.Error($"posX: {PosX}");
                Logger.Error($"lastChar: {LastRune}");
                Logger.Error($"forceSplitData: {ForceSplitData}");
                // Logger.Error($"LineBreaks: {string.Join(", ", LineBreaks)}");

                throw new Exception(
                    "wordStartBreakIndex can only be null if the word begins at a new line," +
                    "in which case this branch shouldn't be reached as" +
                    "the word would be split due to being longer than a single line.");
            }

            breakLine = WordStartBreakIndex.Value.index;
            MaxUsedWidth = Math.Max(MaxUsedWidth, WordStartBreakIndex.Value.lineSize);
        }
        else
        {
            breakLine = null;
            MaxUsedWidth = Math.Max(MaxUsedWidth, PosX);
        }

        return (int)MaxUsedWidth;
    }

    [Pure]
    private static bool IsWordBoundary(Rune a, Rune b)
    {
        return a == new Rune(' ') || b == new Rune(' ') || a == new Rune('-') || b == new Rune('-');
    }

}
