using NUnit.Framework;
using Robust.Shared.Random;

namespace Robust.Shared.Tests.Random;

public sealed class FillStringTests
{
    // A consistent pile of seeds because this is kind of probabilistic.
    private static int[] _seeds =
        Enumerable.Range(1, 128)
            .Select(x => IRobustRandom.CreateSeeded(x).Next())
            .ToArray();

    [Test]
    [TestOf(typeof(RandomExtensions))]
    [TestCaseSource(nameof(_seeds))]
    [Description("""
        Asserts that:
        - Undersized buffers given to FillStringFromRunes throw correctly.
        - FillStringFromRunes outputs a valid string without any broken symbols (i.e. cutting the 𛲜 in half)
        - FillStringFromRunes outputs exactly the set of runes it was given to use, nothing else.
    """)]
    public void FillStringBuffer(int seed)
    {
        var rng = IRobustRandom.CreateSeeded(seed);

        var runes = "abcd𛲜".EnumerateRunes().ToArray();

        Assert.Throws<ArgumentException>(() =>
        {
            var buffer = new char[10];
            rng.FillStringFromRunes(buffer, runes, 10);
        });

        var buffer = new char[20];

        var len = rng.FillStringFromRunes(buffer, runes, 10);

        var cutBuffer = buffer[0..len];

        foreach (var rune in cutBuffer.EnumerateRunes())
        {
            Assert.That(runes, Has.Member(rune));
        }
    }
}
