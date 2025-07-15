using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.PrototypeInstantiationAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
[TestOf(typeof(PrototypeInstantiationAnalyzer))]
public sealed class PrototypeInstantiationAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new RTAnalyzerTest<PrototypeInstantiationAnalyzer>()
        {
            TestState =
            {
                Sources = { code }
            },
        };

        TestHelper.AddEmbeddedSources(
            test.TestState,
            "Robust.Shared.Prototypes.Attributes.cs",
            "Robust.Shared.Prototypes.IPrototype.cs",
            "Robust.Shared.Serialization.Manager.Attributes.DataFieldAttribute.cs"
        );

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    [Test]
    public async Task Test()
    {
        const string code = """
            using Robust.Shared.Serialization;
            using Robust.Shared.Prototypes;

            [Prototype]
            public sealed class FooPrototype : IPrototype
            {
                [IdDataField]
                public string ID { get; private set; } = default!;
            }

            public static class Bad
            {
                public static FooPrototype Real()
                {
                    return new FooPrototype();
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(15,16): warning RA0039: Do not instantiate prototypes directly. Prototypes should always be instantiated by the prototype manager.
            VerifyCS.Diagnostic().WithSpan(15, 16, 15, 34));
    }
}
