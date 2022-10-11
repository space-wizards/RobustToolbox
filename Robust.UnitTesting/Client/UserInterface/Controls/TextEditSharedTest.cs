using NUnit.Framework;
using Robust.Client.UserInterface.Controls;

namespace Robust.UnitTesting.Client.UserInterface.Controls;

[TestFixture]
[TestOf(typeof(TextEditShared))]
[Parallelizable]
internal sealed class TextEditSharedTest
{
    [Test]
    [TestCase("foo bar baz", 1, ExpectedResult = 0)]
    [TestCase("foo bar baz", 4, ExpectedResult = 0)]
    [TestCase("foo bar baz", 5, ExpectedResult = 4)]
    [TestCase("foo bar baz", 8, ExpectedResult = 4)]
    [TestCase("foo bar baz", 9, ExpectedResult = 8)]
    [TestCase("foo +bar baz", 5, ExpectedResult = 4)]
    [TestCase("foo +bar baz", 4, ExpectedResult = 0)]
    [TestCase("foo +bar baz", 6, ExpectedResult = 5)]
    [TestCase("foo +bar baz", 7, ExpectedResult = 5)]
    public int TestPrevWordPosition(string str, int cursor)
    {
        return TextEditShared.PrevWordPosition(str, cursor);
    }

    [Test]
    [TestCase("foo bar baz", 0, ExpectedResult = 4)]
    [TestCase("foo bar baz", 1, ExpectedResult = 4)]
    [TestCase("foo bar baz", 3, ExpectedResult = 4)]
    [TestCase("foo bar baz", 4, ExpectedResult = 8)]
    [TestCase("foo bar baz", 5, ExpectedResult = 8)]
    [TestCase("foo +bar baz", 0, ExpectedResult = 4)]
    [TestCase("foo +bar baz", 4, ExpectedResult = 5)]
    public int TestNextWordPosition(string str, int cursor)
    {
        return TextEditShared.NextWordPosition(str, cursor);
    }
}
