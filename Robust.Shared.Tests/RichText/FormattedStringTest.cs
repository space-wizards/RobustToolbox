using NUnit.Framework;
using Robust.Shared.RichText;

namespace Robust.UnitTesting.Shared.RichText;

[Parallelizable(ParallelScope.All)]
[TestOf(typeof(FormattedString))]
[TestFixture]
internal sealed class FormattedStringTest
{
    /// <summary>
    /// Test that permissive parsing properly normalizes & passes through markup, as appropriate.
    /// </summary>
    [Test]
    [TestCase("", ExpectedResult = "")]
    [TestCase("foobar", ExpectedResult = "foobar")]
    [TestCase("[whaaaaaa", ExpectedResult = "\\[whaaaaaa")]
    [TestCase("\\[whaaaaaa", ExpectedResult = "\\[whaaaaaa")]
    [TestCase("[whaaaaaa]wow[/whaaaaaa]", ExpectedResult = "[whaaaaaa]wow[/whaaaaaa]")]
    [TestCase("[whaaaaaa]\\[womp[/whaaaaaa]", ExpectedResult = "[whaaaaaa]\\[womp[/whaaaaaa]")]
    public static string TestPermissiveNormalize(string input)
    {
        return FormattedString.FromMarkupPermissive(input).Markup;
    }

    [Test]
    [TestCase("")]
    [TestCase("real")]
    [TestCase("[whaaaaaa]wow[/whaaaaaa]")]
    public static void TestStrictParse(string input)
    {
        var str = FormattedString.FromMarkup(input);

        Assert.That(str.Markup, Is.EqualTo(input));
    }

    [Test]
    [TestCase("", ExpectedResult = "")]
    [TestCase("real", ExpectedResult = "real")]
    [TestCase("[real", ExpectedResult = "\\[real")]
    [TestCase("\\", ExpectedResult = @"\\")]
    public static string TestFromPlainText(string input)
    {
        return FormattedString.FromPlainText(input).Markup;
    }

    [Test]
    [TestCase("[whaaaaaawow")]
    [TestCase("[whaaaaaawow val=\"]")]
    public static void TestStrictThrows(string input)
    {
        Assert.That(() => FormattedString.FromMarkup(input), Throws.ArgumentException);
    }
}
