using System;
using Linguini.Syntax.Parser.Error;
using NUnit.Framework;
using Robust.Shared.Localization;

namespace Robust.UnitTesting.Shared.Localization;

[TestFixture]
[Parallelizable]
public sealed class TestFormatErrors
{
    private const string Res1 = "err1 = $user)";
    private const string Res2 = "a = b {{\r\n  err = x";
    private const string Res2Lf = "a = b {{\n  err = x";

    #region ExpectedCrlf

    private const string Expect1Wide1Crlf = "\r\n 99 |err1 = $user)\r\n            ^ Unbalanced closing brace";

    private const string Expect2Wide1Crlf = "\r\n 99 |err1 = $user)\r\n     ^ Unbalanced closing brace";

    private const string Expect1Wide4Crlf =
        "\r\n 99 |err1 = $user)\r\n             ^^^^ Expected a message field for \"x\"";

    private const string Expect2Wide4Crlf = "\r\n 99 |err1 = $user)\r\n     ^^^^ Expected a message field for \"x\"";


    private const string ExpectMulti1Wide1Crlf =
        "\r\n  99 |a = b {{\r\n 100 |  err = x\r\n        ^ Unbalanced closing brace";

    private const string ExpectMulti1Wide3Crlf =
        "\r\n  99 |a = b {{\r\n 100 |  err = x\r\n        ^^^ Expected a message field for \"x\"";

    #endregion

    [Test]
    [TestCase(Expect1Wide1Crlf, Res1, 7)]
    [TestCase(Expect2Wide1Crlf, Res1, 0)]
    [TestCase(Expect1Wide4Crlf, Res1, 8, 12)]
    [TestCase(Expect2Wide4Crlf, Res1, 0, 4)]
    [TestCase(ExpectMulti1Wide1Crlf, Res2, 12)]
    [TestCase(ExpectMulti1Wide3Crlf, Res2, 12, 15)]
    [TestCase(ExpectMulti1Wide1Crlf, Res2Lf, 11)]
    [TestCase(ExpectMulti1Wide3Crlf, Res2Lf, 11, 14)]
    public void TestSingleLineTestCrlf(string expected, string resource, int start, int? end = null)
    {
        var err = ParseError.UnbalancedClosingBrace(start, 99);
        if (end != null)
        {
            err = ParseError.ExpectedMessageField("x".AsMemory(), start, end.Value, 99);
        }

        err.Slice = new Range(0, resource.Length);
        var actual = err.FormatCompileErrors(resource.AsMemory(), "\r\n");
        Assert.That(actual, Is.EqualTo(expected));
    }

    #region ExpectedLf

    private const string Expect1Wide1Lf = "\n 99 |err1 = $user)\n            ^ Unbalanced closing brace";

    private const string Expect2Wide1Lf = "\n 99 |err1 = $user)\n     ^ Unbalanced closing brace";

    private const string Expect1Wide4Lf =
        "\n 99 |err1 = $user)\n             ^^^^ Expected a message field for \"x\"";

    private const string Expect2Wide4Lf = "\n 99 |err1 = $user)\n     ^^^^ Expected a message field for \"x\"";


    private const string ExpectMulti1Wide1Lf = "\n  99 |a = b {{\n 100 |  err = x\n        ^ Unbalanced closing brace";

    private const string ExpectMulti1Wide3Lf =
        "\n  99 |a = b {{\n 100 |  err = x\n        ^^^ Expected a message field for \"x\"";

    #endregion

    [Test]
    [TestCase(Expect1Wide1Lf, Res1, 7)]
    [TestCase(Expect2Wide1Lf, Res1, 0)]
    [TestCase(Expect1Wide4Lf, Res1, 8, 12)]
    [TestCase(Expect2Wide4Lf, Res1, 0, 4)]
    [TestCase(ExpectMulti1Wide1Lf, Res2, 12)]
    [TestCase(ExpectMulti1Wide3Lf, Res2, 12, 15)]
    [TestCase(ExpectMulti1Wide1Lf, Res2Lf, 11)]
    [TestCase(ExpectMulti1Wide3Lf, Res2Lf, 11, 14)]
    public void TestSingleLineTestLf(string expected, string resource, int start, int? end = null)
    {
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
