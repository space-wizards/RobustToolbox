using System;
using System.Linq;
using Linguini.Syntax.Parser.Error;
using NUnit.Framework;
using Pidgin;
using Robust.Shared.Localization;

namespace Robust.UnitTesting.Shared.Localization;

[TestFixture]
[Parallelizable]
public class TestFormatErrors
{
    #region SingleLineExample

    private const string Res1 = "err1 = $user)";

    private const string Expect1Wide1 = @"
err1 = $user)
        ^ Unbalanced closing brace";

    private const string Expect2Wide1 = @"
err1 = $user)
^ Unbalanced closing brace";

    private const string Expect1Wide4 = @"
err1 = $user)
        ^^^^ Expected a message field for ""x""";

    private const string Expect2Wide4 = @"
err1 = $user)
^^^^ Expected a message field for ""x""";

    #endregion

    #region MultiLineExample

    private const string Res2 = @"a = b {{
  err = x ";

    private const string ExpectMulti1Wide1 = @"
a = b {{
  err = x 
  ^ Unbalanced closing brace";

    private const string ExpectMulti1Wide3 = @"
a = b {{
  err = x 
  ^^^ Expected a message field for ""x""";

    #endregion

    [Test]
    [TestCase(Expect1Wide1, Res1, 8)]
    [TestCase(Expect2Wide1, Res1, 0)]
    [TestCase(Expect1Wide4, Res1, 8, 12)]
    [TestCase(Expect2Wide4, Res1, 0, 4)]
    [TestCase(ExpectMulti1Wide1, Res2, 12)]
    [TestCase(ExpectMulti1Wide3, Res2, 12, 15)]
    public void TestSingleLineTest(string expected, string resource, int start, int? end = null)
    {
        var err = ParseError.UnbalancedClosingBrace(start);
        if (end != null)
        {
            err = ParseError.ExpectedMessageField("x".AsMemory(), start, end.Value);
        }

        err.Slice = new Range(0, resource.Length);
        var actual = err.FormatCompileErrors(resource.AsMemory());
        Assert.That(actual, Is.EqualTo(expected));
    }
}
