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
            using System.Collections.Generic;
            using Robust.Shared.Analyzers;

            public class EntityPrototype { };
            public class EntProtoId { };
            public class ReagentPrototype { };

            [PreferOtherType(typeof(EntityPrototype), typeof(EntProtoId))]
            public class ProtoId<T> { };

            public class Test
            {
                public ProtoId<EntityPrototype> Foo = new();
                public List<ProtoId<EntityPrototype>> FooList = new();
                public Dictionary<int, ProtoId<EntityPrototype>> FooDictionary = new();
                public Dictionary<List<ProtoId<EntityPrototype>>, Queue<ProtoId<EntityPrototype>>> FooNested = new();
                public void FooMethod(ProtoId<EntityPrototype> foo) { }
                public void FooListMethod(List<ProtoId<EntityPrototype>> fooList) { }
                public ProtoId<EntityPrototype>? FooReturnMethod() => null;
            }
            """;

        const string fixedCode = """
            using System.Collections.Generic;
            using Robust.Shared.Analyzers;

            public class EntityPrototype { };
            public class EntProtoId { };
            public class ReagentPrototype { };

            [PreferOtherType(typeof(EntityPrototype), typeof(EntProtoId))]
            public class ProtoId<T> { };

            public class Test
            {
                public EntProtoId Foo = new();
                public List<EntProtoId> FooList = new();
                public Dictionary<int, EntProtoId> FooDictionary = new();
                public Dictionary<List<EntProtoId>, Queue<EntProtoId>> FooNested = new();
                public void FooMethod(EntProtoId foo) { }
                public void FooListMethod(List<EntProtoId> fooList) { }
                public EntProtoId? FooReturnMethod() => null;
            }
            """;

        await Verifier(code, fixedCode,
            // /0/Test0.cs(13,12): error RA0031: Use the specific type EntProtoId instead of ProtoId when the type argument is EntityPrototype
            VerifyCS.Diagnostic().WithSpan(13, 12, 13, 36).WithArguments("EntProtoId", "ProtoId", "EntityPrototype"),
            // /0/Test0.cs(14,17): error RA0031: Use the specific type EntProtoId instead of ProtoId when the type argument is EntityPrototype
            VerifyCS.Diagnostic().WithSpan(14, 17, 14, 41).WithArguments("EntProtoId", "ProtoId", "EntityPrototype"),
            // /0/Test0.cs(15,28): error RA0031: Use the specific type EntProtoId instead of ProtoId when the type argument is EntityPrototype
            VerifyCS.Diagnostic().WithSpan(15, 28, 15, 52).WithArguments("EntProtoId", "ProtoId", "EntityPrototype"),
            // /0/Test0.cs(16,61): error RA0031: Use the specific type EntProtoId instead of ProtoId when the type argument is EntityPrototype
            VerifyCS.Diagnostic().WithSpan(16, 61, 16, 85).WithArguments("EntProtoId", "ProtoId", "EntityPrototype"),
            // /0/Test0.cs(16,28): error RA0031: Use the specific type EntProtoId instead of ProtoId when the type argument is EntityPrototype
            VerifyCS.Diagnostic().WithSpan(16, 28, 16, 52).WithArguments("EntProtoId", "ProtoId", "EntityPrototype"),
            // /0/Test0.cs(17,27): error RA0031: Use the specific type EntProtoId instead of ProtoId when the type argument is EntityPrototype
            VerifyCS.Diagnostic().WithSpan(17, 27, 17, 51).WithArguments("EntProtoId", "ProtoId", "EntityPrototype"),
            // /0/Test0.cs(18,36): error RA0031: Use the specific type EntProtoId instead of ProtoId when the type argument is EntityPrototype
            VerifyCS.Diagnostic().WithSpan(18, 36, 18, 60).WithArguments("EntProtoId", "ProtoId", "EntityPrototype"),
            // /0/Test0.cs(19,12): error RA0031: Use the specific type EntProtoId instead of ProtoId when the type argument is EntityPrototype
            VerifyCS.Diagnostic().WithSpan(19, 12, 19, 36).WithArguments("EntProtoId", "ProtoId", "EntityPrototype")
        );
    }
}
