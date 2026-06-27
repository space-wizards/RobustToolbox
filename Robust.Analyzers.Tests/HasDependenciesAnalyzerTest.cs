using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.HasDependenciesAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
public sealed class HasDependenciesAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<HasDependenciesAnalyzer, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { code }
            },
        };

        TestHelper.AddEmbeddedSources(test.TestState, "Robust.Shared.IoC.DependencyAttribute.cs");

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    [Test]
    public async Task ReadOnlyFieldTest()
    {
        const string code = """
            using Robust.Shared.IoC;

            public sealed partial class Foo
            {
                [Dependency] private readonly string _x;
                [Dependency] private string _y;
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(5,26): warning RA0049: Field '_x' is a [Dependency] but is readonly. This will be an error in the future.
            VerifyCS.Diagnostic(HasDependenciesAnalyzer.DiagnosticReadOnly).WithSpan(5, 26, 5, 34).WithArguments("_x")
        );
    }

    [Test]
    public async Task NotPartialTest()
    {
        const string code = """
            using Robust.Shared.IoC;

            public sealed class Foo
            {
                [Dependency] private string _y;
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(3,15): warning RA0048: Type 'Foo' has [Dependency] fields but is not partial. This will be required in the future.
            VerifyCS.Diagnostic(HasDependenciesAnalyzer.DiagnosticNotPartial).WithSpan(3, 15, 3, 20).WithArguments("Foo")
        );
    }

    [Test]
    public async Task NotPartialNestedTest()
    {
        const string code = """
            using Robust.Shared.IoC;

            public sealed class Foo
            {
                public sealed partial class Bar
                {
                    [Dependency] private string _y;
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(5,27): warning RA0049: Type 'Foo.Bar' has [Dependency] fields but is nested in a non-partial type. This will be illegal in the future.
            VerifyCS.Diagnostic(HasDependenciesAnalyzer.DiagnosticNotPartialParent).WithSpan(5, 27, 5, 32).WithArguments("Foo.Bar")
        );
    }

    [Test]
    public async Task NotPropertyField()
    {
        const string code = """
            using Robust.Shared.IoC;

            public sealed partial class Bar
            {
                [field: Dependency] private string _y { get; } = "A";
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(5,5): warning RA0051: Property '_y' has a backing field marked with [Dependency]. This will be an error in the future.
            VerifyCS.Diagnostic(HasDependenciesAnalyzer.DiagnosticPropertyField).WithSpan(5, 5, 5, 58).WithArguments("_y")
        );
    }
}
