using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<Robust.Analyzers.PreferNonGenericVariantForAnalyzer>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
public sealed class PreferNonGenericVariantForTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<PreferNonGenericVariantForAnalyzer, NUnitVerifier>()
        {
            TestState =
            {
                Sources = { code },
            },
        };

        TestHelper.AddEmbeddedSources(
            test.TestState,
            "Robust.Shared.Analyzers.PreferNonGenericVariantForAttribute.cs"
        );

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    [Test]
    public async Task Test()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            public class Bar { };
            public class Baz { };
            public class Okay { };

            public static class Foo
            {
                [PreferNonGenericVariantFor(typeof(Bar), typeof(Baz))]
                public static void DoFoo<T>() { }
            }

            public class Test
            {
                public void DoBad()
                {
                    Foo.DoFoo<Bar>();
                }

                public void DoGood()
                {
                    Foo.DoFoo<Okay>();
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(17,9): warning RA0029: Use the non-generic variant of this method for type Bar
            VerifyCS.Diagnostic().WithSpan(17, 9, 17, 25).WithArguments("Bar")
        );
    }
}
