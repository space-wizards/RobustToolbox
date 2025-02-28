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
                public static void DirtyFields<T>(T comp, [ValidateMember]ReadOnlySpan<string> fieldNames) { }
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

                    TestManager.DirtyTwoFields(testComp, nameof(OtherComponent.FloatField), nameof(OtherComponent.DoubleField));

                    TestManager.DirtyFields(testComp, [nameof(TestComponent.IntField), nameof(TestComponent.BoolField)]);

                    TestManager.DirtyFields(testComp, [nameof(TestComponent.IntField), nameof(OtherComponent.FloatField)]);

                    TestManager.DirtyField(testComp, "foobar");

                    TestManager.DirtyFields(testComp, ["foobar", "bizbaz"]);
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(32,42): error RA0033: FloatField is not a member of TestComponent
            VerifyCS.Diagnostic().WithSpan(32, 42, 32, 75).WithArguments("FloatField", "TestComponent"),
            // /0/Test0.cs(34,43): error RA0033: IntField is not a member of OtherComponent
            VerifyCS.Diagnostic().WithSpan(34, 43, 34, 73).WithArguments("IntField", "OtherComponent"),
            // /0/Test0.cs(40,78): error RA0033: FloatField is not a member of TestComponent
            VerifyCS.Diagnostic().WithSpan(40, 78, 40, 111).WithArguments("FloatField", "TestComponent"),
            // /0/Test0.cs(42,46): error RA0033: FloatField is not a member of TestComponent
            VerifyCS.Diagnostic().WithSpan(42, 46, 42, 79).WithArguments("FloatField", "TestComponent"),
            // /0/Test0.cs(42,81): error RA0033: DoubleField is not a member of TestComponent
            VerifyCS.Diagnostic().WithSpan(42, 81, 42, 115).WithArguments("DoubleField", "TestComponent"),
            // /0/Test0.cs(46,43): error RA0033: FloatField is not a member of TestComponent
            VerifyCS.Diagnostic().WithSpan(46, 43, 46, 110).WithArguments("FloatField", "TestComponent"),
            // /0/Test0.cs(48,42): error RA0033: foobar is not a member of TestComponent
            VerifyCS.Diagnostic().WithSpan(48, 42, 48, 50).WithArguments("foobar", "TestComponent"),
            // /0/Test0.cs(50,43): error RA0033: bizbaz is not a member of TestComponent
            VerifyCS.Diagnostic().WithSpan(50, 43, 50, 63).WithArguments("bizbaz", "TestComponent"),
            // /0/Test0.cs(50,43): error RA0033: foobar is not a member of TestComponent
            VerifyCS.Diagnostic().WithSpan(50, 43, 50, 63).WithArguments("foobar", "TestComponent")
        );
    }
}
