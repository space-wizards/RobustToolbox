using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.CustomBaseTypeSerializerAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
public sealed class CustomBaseTypeSerializerAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<CustomBaseTypeSerializerAnalyzer, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { code }
            },
        };

        test.TestState.Sources.Add(("TestTypeDefs.cs", TestTypeDefs));

        TestHelper.AddEmbeddedSources(
            test.TestState
        );

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    private const string TestTypeDefs = """
            using System;

            namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

            public sealed class CustomBaseTypeSerializer<TBase>;
            """;

    [Test]
    public async Task Test()
    {
        const string code = """
            using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;
            internal abstract partial class TestTypeBaseWrong;
            internal sealed partial class Test
            {
                public Test()
                {
                    var type = typeof(CustomBaseTypeSerializer<TestTypeBaseWrong>);
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(7,20): error RA0045: Type parameter TestTypeBaseWrong does not end with 'Base'
            VerifyCS.Diagnostic().WithSpan(7, 20, 7, 71).WithArguments("TestTypeBaseWrong"));
    }
}
