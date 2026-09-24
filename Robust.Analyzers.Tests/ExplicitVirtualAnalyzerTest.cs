using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.ExplicitVirtualAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
[TestOf(typeof(ExplicitVirtualAnalyzer))]
public sealed class ExplicitVirtualAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<ExplicitVirtualAnalyzer, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { code },
            },
        };

        TestHelper.AddEmbeddedSources(
            test.TestState,
            "Robust.Shared.Analyzers.VirtualAttribute.cs"
        );

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    [Test]
    [Description("Ensures that a non-sealed/abstract/static class not marked as [Virtual] raises a warning.")]
    public async Task NoVirtualOrOther()
    {
        const string code = """
            public class Foo { }
            """;

        await Verifier(code,
            // /0/Test0.cs(1,8): warning RA0003: Class must be explicitly marked as [Virtual], abstract, static, or sealed
            VerifyCS.Diagnostic(ExplicitVirtualAnalyzer.ExplicitVirtualRule).WithSpan(1, 8, 1, 13)
        );
    }

    [Test]
    [Description("Ensures that a non-sealed/abstract/static class explicitly marked as [Virtual] does not raise a warning.")]
    public async Task OnlyVirtual()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            [Virtual]
            public class Foo { }
            """;

        await Verifier(code, []);
    }

    [Test]
    [Description("Ensures that a sealed class marked as [Virtual] raises an error.")]
    public async Task SealedVirtual()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            [Virtual]
            public sealed class Foo { }
            """;

        await Verifier(code,
            // /0/Test0.cs(4,15): error RA0048: A class marked as [Virtual] cannot be abstract, static, or sealed
            VerifyCS.Diagnostic(ExplicitVirtualAnalyzer.ExclusiveRule).WithSpan(4, 15, 4, 20));
    }

    [Test]
    [Description("Ensures that an abstract class marked as [Virtual] raises an error.")]
    public async Task AbstractVirtual()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            [Virtual]
            public abstract class Foo { }
            """;

        await Verifier(code,
            // /0/Test0.cs(4,17): error RA0048: A class marked as [Virtual] cannot be abstract, static, or sealed
            VerifyCS.Diagnostic(ExplicitVirtualAnalyzer.ExclusiveRule).WithSpan(4, 17, 4, 22));
    }

    [Test]
    [Description("Ensures that a static class marked as [Virtual] raises an error.")]
    public async Task StaticVirtual()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            [Virtual]
            public static class Foo { }
            """;

        await Verifier(code,
            // /0/Test0.cs(4,15): error RA0048: A class marked as [Virtual] cannot be abstract, static, or sealed
            VerifyCS.Diagnostic(ExplicitVirtualAnalyzer.ExclusiveRule).WithSpan(4, 15, 4, 20));
    }
}
