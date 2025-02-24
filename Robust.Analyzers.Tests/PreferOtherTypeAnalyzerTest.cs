using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.PreferOtherTypeAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
public sealed class PreferOtherTypeAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<PreferOtherTypeAnalyzer, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { code },
            },
        };

        TestHelper.AddEmbeddedSources(
            test.TestState,
            "Robust.Shared.Analyzers.PreferOtherTypeAttribute.cs"
        );

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    [Test]
    public async Task Test()
    {
        const string code = """
            using System.Collections.Generic;
            using Robust.Shared.Analyzers;

            public class EntityPrototype { };
            public class EntProtoId { };
            public class ReagentPrototype { };

            [PreferOtherType(typeof(EntityPrototype), typeof(EntProtoId))]
            public class ProtoId<T> { };

            public class Test
            {
                public ProtoId<EntityPrototype> Bad = new();

                public ProtoId<ReagentPrototype> Good = new();

                public List<ProtoId<EntityPrototype>> BadList = new();

                public List<ProtoId<ReagentPrototype>> GoodList = new();

                public Dictionary<int, ProtoId<EntityPrototype>> BadDictionary = new();

                public Dictionary<int, ProtoId<ReagentPrototype>> GoodDictionary = new();

                public List<HashSet<Queue<ProtoId<EntityPrototype>>>> BadNested = new();

                public List<HashSet<Queue<ProtoId<ReagentPrototype>>>> GoodNested = new();
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(13,12): error RA0031: Use the specific type EntProtoId instead of ProtoId when the type argument is EntityPrototype
            VerifyCS.Diagnostic().WithSpan(13, 12, 13, 36).WithArguments("EntProtoId", "ProtoId", "EntityPrototype"),
            // /0/Test0.cs(17,17): error RA0031: Use the specific type EntProtoId instead of ProtoId when the type argument is EntityPrototype
            VerifyCS.Diagnostic().WithSpan(17, 17, 17, 41).WithArguments("EntProtoId", "ProtoId", "EntityPrototype"),
            // /0/Test0.cs(21,28): error RA0031: Use the specific type EntProtoId instead of ProtoId when the type argument is EntityPrototype
            VerifyCS.Diagnostic().WithSpan(21, 28, 21, 52).WithArguments("EntProtoId", "ProtoId", "EntityPrototype"),
            // /0/Test0.cs(25,31): error RA0031: Use the specific type EntProtoId instead of ProtoId when the type argument is EntityPrototype
            VerifyCS.Diagnostic().WithSpan(25, 31, 25, 55).WithArguments("EntProtoId", "ProtoId", "EntityPrototype")
        );
    }
}
