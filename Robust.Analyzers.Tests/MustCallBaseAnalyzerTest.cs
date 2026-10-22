using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.MustCallBaseAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
public sealed class MustCallBaseAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<MustCallBaseAnalyzer, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { code }
            },
        };

        TestHelper.AddEmbeddedSources(
            test.TestState,
            "Robust.Shared.IoC.MustCallBaseAttribute.cs"
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

            public class Foo
            {
                [MustCallBase]
                public virtual void Function()
                {

                }

                [MustCallBase(true)]
                public virtual void Function2()
                {

                }
            }

            public class Bar : Foo
            {
                public override void Function()
                {

                }

                public override void Function2()
                {

                }
            }

            public class Baz : Foo
            {
                public override void Function()
                {
                    base.Function();
                }
            }

            public class Bal : Bar
            {
                public override void Function2()
                {
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(20,26): warning RA0028: Overriders of this function must always call the base function
            VerifyCS.Diagnostic().WithSpan(20, 26, 20, 34),
            // /0/Test0.cs(41,26): warning RA0028: Overriders of this function must always call the base function
            VerifyCS.Diagnostic().WithSpan(41, 26, 41, 35));
    }
}
