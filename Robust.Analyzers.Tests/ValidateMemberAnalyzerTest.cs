using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.ValidateMemberAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;


namespace Robust.Analyzers.Tests;

public sealed class ValidateMemberAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<ValidateMemberAnalyzer, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { code },
            },
        };

        TestHelper.AddEmbeddedSources(
            test.TestState,
            "Robust.Shared.Analyzers.ValidateMemberAttribute.cs"
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
            using Robust.Shared.Analyzers;

            public sealed class TestComponent
            {
                public int IntField;
                public bool BoolField;
            }

            public sealed class OtherComponent
            {
                public float FloatField;
                public double DoubleField;
            }

            public sealed class TestManager
            {
                public static void DirtyField<T>(T comp, [ValidateMember]string fieldName) { }
                public static void DirtyTwoFields<T>(T comp, [ValidateMember]string first, [ValidateMember]string second) { }
            }

            public sealed class TestCaller
            {
                public void Test()
                {
                    var testComp = new TestComponent();
                    var otherComp = new OtherComponent();

                    TestManager.DirtyField(testComp, nameof(TestComponent.IntField));

                    TestManager.DirtyField(testComp, nameof(OtherComponent.FloatField));

                    TestManager.DirtyField(otherComp, nameof(TestComponent.IntField));

                    TestManager.DirtyField(otherComp, nameof(OtherComponent.FloatField));

                    TestManager.DirtyTwoFields(testComp, nameof(TestComponent.IntField), nameof(TestComponent.BoolField));

                    TestManager.DirtyTwoFields(testComp, nameof(TestComponent.IntField), nameof(OtherComponent.FloatField));
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(31,42): error RA0033: FloatField is not a member of TestComponent
            VerifyCS.Diagnostic().WithSpan(31, 42, 31, 75).WithArguments("FloatField", "TestComponent"),
            // /0/Test0.cs(33,43): error RA0033: IntField is not a member of OtherComponent
            VerifyCS.Diagnostic().WithSpan(33, 43, 33, 73).WithArguments("IntField", "OtherComponent"),
            // /0/Test0.cs(39,78): error RA0033: FloatField is not a member of TestComponent
            VerifyCS.Diagnostic().WithSpan(39, 78, 39, 111).WithArguments("FloatField", "TestComponent")
        );
    }
}
