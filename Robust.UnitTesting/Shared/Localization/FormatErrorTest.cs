using System;
using Linguini.Syntax.Parser.Error;
using NUnit.Framework;
using Robust.Shared.Localization;

namespace Robust.UnitTesting.Shared.Localization;

[TestFixture]
[Parallelizable]
public sealed class TestFormatErrors
{
    // Error spans a single line
    private const string ResSingle = "err1 = $user)";

    private const string ExpectSingle1Wide1 = """
         1 |err1 = $user)
                   ^ Unbalanced closing brace
        """;

    private const string Expect2Wide1 = """
         1 |err1 = $user)
            ^ Unbalanced closing brace
        """;

    private const string Expect1Wide4 = """
         1 |err1 = $user)
                    ^^^^ Expected a message field for "x"
        """;

    private const string Expect2Wide4 = """
         1 |err1 = $user)
            ^^^^ Expected a message field for "x"
        """;

    // Error spans multiple lines
    private const string ResMulti = """
        a = b {{
          err = x
        """;

    private const string ExpectMulti1Wide1 = """
         1 |a = b {{
         2 |  err = x
              ^ Unbalanced closing brace
        """;

    private const string ExpectMulti1Wide3 = """
         1 |a = b {{
         2 |  err = x
              ^^^ Expected a message field for "x"
        """;

    [Test]
    [TestCase(ExpectSingle1Wide1, ResSingle, 7)]
    [TestCase(Expect2Wide1, ResSingle, 0)]
    [TestCase(Expect1Wide4, ResSingle, 8, 12)]
    [TestCase(Expect2Wide4, ResSingle, 0, 4)]
    [TestCase(ExpectMulti1Wide1, ResMulti, 11)]
    [TestCase(ExpectMulti1Wide3, ResMulti, 11, 14)]
    [TestCase(ExpectMulti1Wide1, ResMulti, 11)]
    [TestCase(ExpectMulti1Wide3, ResMulti, 11, 14)]
    public void TestSingleLineTestLf(string expected, string resource, int start, int? end = null)
    {
        expected = expected.ReplaceLineEndings("\n");
        resource = resource.ReplaceLineEndings("\n");

        var err = ParseError.UnbalancedClosingBrace(start, 99);
        if (end != null)
        {
            err = ParseError.ExpectedMessageField("x".AsMemory(), start, end.Value, 99);
        }

        err.Slice = new Range(0, resource.Length);
        var actual = err.FormatCompileErrors(resource.AsMemory(), "\n");
        Assert.That(actual, Is.EqualTo(expected));
    }
}
