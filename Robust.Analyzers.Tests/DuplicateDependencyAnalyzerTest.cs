using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.DuplicateDependencyAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
[TestOf(typeof(DuplicateDependencyAnalyzer))]
public sealed class DuplicateDependencyAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<DuplicateDependencyAnalyzer, DefaultVerifier>()
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
                private object? Field2;

                [Dependency]
                private string? DifferentField;

                private string? NonDependency1;
                private string? NonDependency2;
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(9,21): warning RA0032: Another [Dependency] field of type 'object?' already exists in this type as field 'Field'
            VerifyCS.Diagnostic().WithSpan(9, 21, 9, 27).WithArguments("object?", "Field"));
    }
}
