using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.DependencyNullableAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture, TestOf(typeof(DependencyNullableAnalyzer))]
public sealed class DependencyNullableAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<DependencyNullableAnalyzer, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { code }
            },
        };

        TestHelper.AddEmbeddedSources(
            test.TestState,
            "Robust.Shared.IoC.DependencyAttribute.cs"
        );

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    [Test]
    public async Task Test()
    {
        const string code = """
            using Robust.Shared.IoC;

            public sealed class Foo
            {
                [Dependency]
                private object? Field;

                [Dependency]
                private object FieldCorrect = null!;
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(6,13): warning RA0048: [Dependency] field 'Field' is a nullable type. This has no effect and will be disallowed in the future.
            VerifyCS.Diagnostic().WithSpan(6, 13, 6, 20).WithArguments("Field"));
    }
}
