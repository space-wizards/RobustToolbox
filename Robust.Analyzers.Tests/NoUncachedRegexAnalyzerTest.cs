using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.NoUncachedRegexAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
public sealed class NoUncachedRegexAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<NoUncachedRegexAnalyzer, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { code }
            },
        };

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    [Test]
    public async Task Test()
    {
        const string code = """
            using System.Text.RegularExpressions;

            public static class Foo
            {
                public static void Bad()
                {
                    Regex.Replace("foo", "bar", "baz");
                }

                public static void Good()
                {
                    var r = new Regex("bar");
                    r.Replace("foo", "baz");
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(7,9): warning RA0026: Usage of a static Regex function that takes in a pattern string. This can cause constant re-parsing of the pattern.
            VerifyCS.Diagnostic().WithSpan(7, 9, 7, 43)
        );
    }
}
