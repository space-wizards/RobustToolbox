using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.PreferOtherTypeAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

public sealed class PreferOtherTypeFixerTest
{
    private static Task Verifier(string code, string fixedCode, params DiagnosticResult[] expected)
    {
        var test = new CSharpCodeFixTest<PreferOtherTypeAnalyzer, PreferOtherTypeFixer, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { code },
            },
            FixedState =
            {
                Sources = { fixedCode },
            }
        };

        TestHelper.AddEmbeddedSources(
            test.TestState,
            "Robust.Shared.Analyzers.PreferOtherTypeAttribute.cs"
        );

        TestHelper.AddEmbeddedSources(
            test.FixedState,
            "Robust.Shared.Analyzers.PreferOtherTypeAttribute.cs"
        );

        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    [Test]
    public async Task Test()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            public class EntityPrototype { };
            public class EntProtoId { };
            public class ReagentPrototype { };

            [PreferOtherType(typeof(EntityPrototype), typeof(EntProtoId))]
            public class ProtoId<T> { };

            public class Test
            {
                public ProtoId<EntityPrototype> Foo = new();
            }
            """;

        const string fixedCode = """
            using Robust.Shared.Analyzers;

            public class EntityPrototype { };
            public class EntProtoId { };
            public class ReagentPrototype { };

            [PreferOtherType(typeof(EntityPrototype), typeof(EntProtoId))]
            public class ProtoId<T> { };

            public class Test
            {
                public EntProtoId Foo = new();
            }
            """;

        await Verifier(code, fixedCode,
        // /0/Test0.cs(12,12): error RA0031: Use the specific type EntProtoId instead of ProtoId when the type argument is EntityPrototype
        VerifyCS.Diagnostic().WithSpan(12, 12, 12, 48).WithArguments("EntProtoId", "ProtoId", "EntityPrototype"));
    }
}
