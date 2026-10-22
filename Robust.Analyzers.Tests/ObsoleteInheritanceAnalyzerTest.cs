using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.ObsoleteInheritanceAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

/// <summary>
/// Analyzer that implements <c>[ObsoleteInheritance]</c> checking, to give obsoletion warnings for inheriting types
/// that should never have been virtual.
/// </summary>
[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
public sealed class ObsoleteInheritanceAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<ObsoleteInheritanceAnalyzer, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { code },
            },
        };

        TestHelper.AddEmbeddedSources(
            test.TestState,
            "Robust.Shared.Analyzers.ObsoleteInheritanceAttribute.cs"
        );

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    [Test]
    public async Task TestBasic()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            [ObsoleteInheritance]
            public class Base;

            public class NotAllowed : Base;
            """;

        await Verifier(code,
            // /0/Test0.cs(6,14): warning RA0034: Type 'NotAllowed' inherits from 'Base', which has obsoleted inheriting from itself
            VerifyCS.Diagnostic(ObsoleteInheritanceAnalyzer.Rule).WithSpan(6, 14, 6, 24).WithArguments("NotAllowed", "Base")
            );
    }

    [Test]
    public async Task TestMessage()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            [ObsoleteInheritance("Sus")]
            public class Base;

            public class NotAllowed : Base;
            """;

        await Verifier(code,
            // /0/Test0.cs(6,14): warning RA0034: Type 'NotAllowed' inherits from 'Base', which has obsoleted inheriting from itself: "Sus"
            VerifyCS.Diagnostic(ObsoleteInheritanceAnalyzer.RuleWithMessage).WithSpan(6, 14, 6, 24).WithArguments("NotAllowed", "Base", "Sus")
        );
    }

    [Test]
    public async Task TestNormal()
    {
        const string code = """
            public class Base;

            public class AllowedAllowed : Base;
            """;

        await Verifier(code);
    }
}
