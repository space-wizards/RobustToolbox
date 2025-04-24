using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.DependencyAssignAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
public sealed class DependencyAssignAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<DependencyAssignAnalyzer, DefaultVerifier>()
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

                public Foo()
                {
                    Field = "A";
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(10,9): warning RA0025: Tried to assign to [Dependency] field 'Field'. Remove [Dependency] or inject it via field injection instead.
            VerifyCS.Diagnostic().WithSpan(10, 9, 10, 20).WithArguments("Field"));
    }
}
