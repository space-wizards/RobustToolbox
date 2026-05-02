using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using Robust.Roslyn.Shared;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.YamlTagShortenerAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
public sealed class YamlTagShortenerAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<YamlTagShortenerAnalyzer, DefaultVerifier>()
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
            namespace Robust.Shared.Serialization.Manager.Attributes
            {
                public sealed class YamlTagShortenerAttribute : Attribute;
                public sealed class CustomChildTagAttribute<T> : Attribute
                {
                    public CustomChildTagAttribute(string tag) { }
                }
            }
            """;

    /// <summary>
    /// Test that the base class ends in base when the YamlTagShortener is applied.
    /// </summary>
    [Test]
    public async Task TestMustEndWithBase()
    {
        const string code = """
            using Robust.Shared.Serialization.Manager.Attributes;

            [YamlTagShortener]
            internal abstract partial class TestTypeBaseWrong;
            """;

        await Verifier(code,
            // /0/Test0.cs(3,1): error RA0048: [YamlTagShortener] usage on type TestTypeBaseWrong is incorrect
            VerifyCS.Diagnostic(Diagnostics.IdTypeEndsWithBase).WithSpan(3, 1, 4, 51).WithArguments("TestTypeBaseWrong"));
    }

    /// <summary>
    /// Test that incorrectly named child types are detected when using [YamlTagShortener].
    /// </summary>
    [Test]
    public async Task TestWrongChildNameForYamlTagShortener()
    {
        const string code = """
            using Robust.Shared.Serialization.Manager.Attributes;

            [YamlTagShortener]
            internal abstract partial class TestTypeBase;

            internal abstract partial class TestWrongType : TestTypeBase;
            """;

        await Verifier(code,
            // /0/Test0.cs(6,1): error RA0049: Base type uses [YamlTagShortener] but the name of this type TestWrongType is not supported
            VerifyCS.Diagnostic(Diagnostics.IdYamlTagShortenerUnsupportedChildName).WithSpan(6, 1, 6, 62).WithArguments("TestWrongType"));
    }

    /// <summary>
    /// Test that other incorrectly named child types are detected when using [CustomChildTag].
    /// </summary>
    [Test]
    public async Task TestWrongChildNameForCustomChildTag()
    {
        const string code = """
            using Robust.Shared.Serialization.Manager.Attributes;

            [CustomChildTag<TestWrongTypeA>("A")]
            internal abstract partial class TestTypeBase;

            internal abstract partial class TestWrongTypeA : TestTypeBase;
            internal abstract partial class TestWrongTypeB : TestTypeBase;
            """;

        await Verifier(code,
            // /0/Test0.cs(7,1): error RA0049: Base type uses the YamlTagShortener but the name of this type TestWrongTypeB is not supported
            VerifyCS.Diagnostic(Diagnostics.IdYamlTagShortenerUnsupportedChildName).WithSpan(7, 1, 7, 63).WithArguments("TestWrongTypeB"));
    }

    /// <summary>
    /// Test that usage of [CustomChildTag] allows the base type to not end in 'Base'.
    /// </summary>
    [Test]
    public async Task TestCustomChildTagAllowsNotEndingInBase()
    {
        const string code = """
            using Robust.Shared.Serialization.Manager.Attributes;

            public class SomeTypeA;

            [YamlTagShortener]
            [CustomChildTag<SomeTypeA>("A")]
            internal abstract partial class TestTypeBaseWrong;
            """;

        // Expect zero diagnostics.
        await Verifier(code);
    }

    /// <summary>
    /// Test that usage of [CustomChildTag] allows the base type to not end in 'Base'.
    /// </summary>
    [Test]
    public async Task TestCustomChildTagAllowsWrongChildName()
    {
        const string code = """
            using Robust.Shared.Serialization.Manager.Attributes;

            [YamlTagShortener]
            [CustomChildTag<SomeTypeA>("A")]
            internal abstract partial class TestTypeBase;

            internal abstract partial class SomeTypeA : TestTypeBase;
            """;

        // Expect zero diagnostics.
        await Verifier(code);
    }
}
