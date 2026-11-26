using NUnit.Framework;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using System.Text;

namespace Robust.Client.Tests.UserInterface;

[Parallelizable(ParallelScope.All)]
public sealed class WordWrapTest
{
    private List<int> GenerateBreaks(string s, int maxWidth)
    {
        var breaksOut = new List<int>();
        var wrapper = new WordWrap(maxSizeX: maxWidth);

        // For simplicity, assume every character has the same width, except for some special ones
        var charMetrics = new CharMetrics (bearingX: 0, bearingY: 0, advance: 10, width: 10, height: 10);
        var wideMetrics = new CharMetrics (bearingX: 0, bearingY: 0, advance: 25, width: 25, height: 10);
        var narrowMetrics = new CharMetrics (bearingX: 0, bearingY: 0, advance: 6, width: 6, height: 10);

        foreach (var r in s.EnumerateRunes())
        {
            wrapper.NextRune(r, out var breakLine, out var breakNewLine, out var skip);
            if (breakLine != null)
            {
                breaksOut.Add(breakLine.Value);
            }
            if (breakNewLine != null)
            {
                breaksOut.Add(breakNewLine.Value);
            }
            if (skip)
            {
                continue;
            }

            var metrics = charMetrics;
            if (r == new Rune('W'))
            {
                metrics = wideMetrics;
            }
            else if (r == new Rune('|'))
            {
                metrics = narrowMetrics;
            }

            wrapper.NextMetrics(metrics, out breakLine, out var abort);

            if (breakLine != null)
            {
                breaksOut.Add(breakLine.Value);
            }
            if (abort)
            {
                return breaksOut;
            }
        }

        return breaksOut;
    }

    [Test]
    // Basic wrapping. First two words fit on one line, need a break to fit the third
    //Breaks at:   v
    [TestCase("1 3 123", 50, new int[]{4})]
    // Basic wrapping, over more lines:
    //Breaks at:   v     v
    [TestCase("1 3 123 5 1234", 50, new int[]{4, 10})]
    // Word doesn't fit on one line, need to break mid-word
    //Breaks at:    v
    [TestCase("12345123", 50, new int[]{5})]
    // Word doesn't fit on *two* lines, needs two breaks mid-word
    //Breaks at:    v    v
    [TestCase("1234512345123", 50, new int[]{5, 10})]
    // Same,  but with some words at the start
    //Breaks at:   v    v    v
    [TestCase("1 3 12345123451", 50, new int[]{4, 9, 14})]
    // Can fit first two words on one line, need a break for the third word and needs splitting mid-word
    //Breaks at:   v    v
    [TestCase("1 3 12345123", 50, new int[]{4, 9})]
    // Check for a debug assert in WordWrap. Second word needs an extra split on the last character
    //Breaks at:   v   v
    [TestCase("123 1|34W ", 50, new int[]{4, 8})]
    public void TestSimpleWrapping(string s, int maxWidth, int[] expectedBreaks)
    {
        var breaks = GenerateBreaks(s, maxWidth);
        Assert.That(breaks, Is.EqualTo(expectedBreaks));
    }
}
