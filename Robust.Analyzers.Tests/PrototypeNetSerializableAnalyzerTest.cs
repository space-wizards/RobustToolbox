using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.PrototypeNetSerializableAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
[TestOf(typeof(PrototypeNetSerializableAnalyzer))]
public sealed class PrototypeNetSerializableAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new RTAnalyzerTest<PrototypeNetSerializableAnalyzer>()
        {
            TestState =
            {
                Sources = { code }
            },
        };

        TestHelper.AddEmbeddedSources(
            test.TestState,
            "Robust.Shared.Serialization.NetSerializableAttribute.cs",
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
            using System;
            using Robust.Shared.Serialization;
            using Robust.Shared.Prototypes;

            [Prototype]
            [Serializable, NetSerializable]
            public sealed class FooPrototype : IPrototype
            {
                [IdDataField]
                public string ID { get; private set; } = default!;
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(7,21): warning RA0037: Type FooPrototype is a prototype and marked as [NetSerializable]. Prototypes should not be directly sent over the network, send their IDs instead.
            VerifyCS.Diagnostic(PrototypeNetSerializableAnalyzer.RuleNetSerializable).WithSpan(7, 21, 7, 33).WithArguments("FooPrototype"),
            // /0/Test0.cs(7,21): warning RA0038: Type FooPrototype is a prototype and marked as [Serializable]. Prototypes should not be directly sent over the network, send their IDs instead.
            VerifyCS.Diagnostic(PrototypeNetSerializableAnalyzer.RuleSerializable).WithSpan(7, 21, 7, 33).WithArguments("FooPrototype"));
    }
}
