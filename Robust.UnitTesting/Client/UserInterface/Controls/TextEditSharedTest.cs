using NUnit.Framework;
using Robust.Client.UserInterface.Controls;

namespace Robust.UnitTesting.Client.UserInterface.Controls;

[TestFixture]
[TestOf(typeof(TextEditShared))]
[Parallelizable]
internal sealed class TextEditSharedTest
{
    // @formatter:off
    [Test]
    [TestCase("foo bar baz",   0,  ExpectedResult = 0)]
    [TestCase("foo bar baz",   1,  ExpectedResult = 0)]
    [TestCase("foo bar baz",   4,  ExpectedResult = 0)]
    [TestCase("foo bar baz",   5,  ExpectedResult = 4)]
    [TestCase("foo bar baz",   8,  ExpectedResult = 4)]
    [TestCase("foo bar baz",   9,  ExpectedResult = 8)]
    [TestCase("foo +bar baz",  5,  ExpectedResult = 4)]
    [TestCase("foo +bar baz",  4,  ExpectedResult = 0)]
    [TestCase("foo +bar baz",  6,  ExpectedResult = 5)]
    [TestCase("foo +bar baz",  7,  ExpectedResult = 5)]
    [TestCase("Foo Bar Baz",   4,  ExpectedResult = 0)]
    [TestCase("Foo Bar Baz",   11, ExpectedResult = 8)]
    [TestCase("Foo[Bar[Baz",   3,  ExpectedResult = 0)]
    [TestCase("Foo[Bar[Baz",   4,  ExpectedResult = 3)]
    [TestCase("Foo^Bar^Baz",   3,  ExpectedResult = 0)]
    [TestCase("Foo^Bar^Baz",   5,  ExpectedResult = 3)]
    [TestCase("Foo^^^Bar^Baz", 9,  ExpectedResult = 3)]
    [TestCase("^^^ ^^^",       7,  ExpectedResult = 0)]
    [TestCase("^^^ ^^^",       13, ExpectedResult = 7)]
    // @formatter:on
    public int TestPrevWordPosition(string str, int cursor)
    {
        // For my sanity.
        str = str.Replace("^", "👏");

        return TextEditShared.PrevWordPosition(str, cursor);
    }

    [Test]
    // @formatter:off
    [TestCase("foo bar baz",   11, ExpectedResult = 11)]
    [TestCase("foo bar baz",   0,  ExpectedResult = 4 )]
    [TestCase("foo bar baz",   1,  ExpectedResult = 4 )]
    [TestCase("foo bar baz",   3,  ExpectedResult = 4 )]
    [TestCase("foo bar baz",   4,  ExpectedResult = 8 )]
    [TestCase("foo bar baz",   5,  ExpectedResult = 8 )]
    [TestCase("Foo Bar Baz",   0,  ExpectedResult = 4 )]
    [TestCase("Foo Bar Baz",   8,  ExpectedResult = 11)]
    [TestCase("foo +bar baz",  0,  ExpectedResult = 4 )]
    [TestCase("foo +bar baz",  4,  ExpectedResult = 5 )]
    [TestCase("Foo[Bar[Baz",   0,  ExpectedResult = 3 )]
    [TestCase("Foo[Bar[Baz",   3,  ExpectedResult = 4 )]
    [TestCase("Foo^Bar^Baz",   0,  ExpectedResult = 3 )]
    [TestCase("Foo^Bar^Baz",   3,  ExpectedResult = 5 )]
    [TestCase("Foo^^^Bar^Baz", 3,  ExpectedResult = 9 )]
    [TestCase("^^^ ^^^",       0,  ExpectedResult = 7 )]
    [TestCase("^^^ ^^^",       7,  ExpectedResult = 13)]
    // @formatter:on
    public int TestNextWordPosition(string str, int cursor)
    {
        // For my sanity.
        str = str.Replace("^", "👏");

        return TextEditShared.NextWordPosition(str, cursor);
    }
}
