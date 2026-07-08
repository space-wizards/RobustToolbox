using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.ForbidLiteralAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;


namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
public sealed class ForbidLiteralAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<ForbidLiteralAnalyzer, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { code },
            },
        };

        TestHelper.AddEmbeddedSources(
            test.TestState,
            "Robust.Shared.Analyzers.ForbidLiteralAttribute.cs"
        );

        test.TestState.Sources.Add(("TestTypeDefs.cs", TestTypeDefs));

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    private const string TestTypeDefs = """
        using System.Collections.Generic;
        using Robust.Shared.Analyzers;

        public sealed class TestClass
        {
            public static void OneParameterForbidden([ForbidLiteral] string value) { }
            public static void TwoParametersFirstForbidden([ForbidLiteral] string first, string second) { }
            public static void TwoParametersBothForbidden([ForbidLiteral] string first, [ForbidLiteral] string second) { }
            public static void ListParameterForbidden([ForbidLiteral] List<string> values) { }
            public static void ParamsListParameterForbidden([ForbidLiteral] params List<string> values) { }
        }

        public record struct StringWrapper(string value)
        {
            private readonly string _value = value;

            public static implicit operator string(StringWrapper wrapper)
            {
                return wrapper._value;
            }
        }
    """;

    [Test]
    public async Task TestOneParameter()
    {
        const string code = """
            public sealed class Tester
            {
                private const string _constValue = "foo";
                private static readonly string StaticValue = "bar";
                private static readonly StringWrapper WrappedValue = new("biz");

                public void Test()
                {
                    TestClass.OneParameterForbidden(_constValue);
                    TestClass.OneParameterForbidden(StaticValue);
                    TestClass.OneParameterForbidden(WrappedValue);
                    TestClass.OneParameterForbidden("baz");
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(12,41): error RA0033: The "value" parameter of OneParameterForbidden forbids literal values
            VerifyCS.Diagnostic().WithSpan(12, 41, 12, 46).WithArguments("value", "OneParameterForbidden")
        );
    }

    [Test]
    public async Task TestTwoParametersFirstForbidden()
    {
        const string code = """
            public sealed class Tester
            {
                private const string _constValue = "foo";

                public void Test()
                {
                    TestClass.TwoParametersFirstForbidden(_constValue, "whatever");
                    TestClass.TwoParametersFirstForbidden(_constValue, _constValue);
                    TestClass.TwoParametersFirstForbidden("foo", "whatever");
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(9,47): error RA0033: The "first" parameter of TwoParametersFirstForbidden forbids literal values
            VerifyCS.Diagnostic().WithSpan(9, 47, 9, 52).WithArguments("first", "TwoParametersFirstForbidden")
        );
    }

    [Test]
    public async Task TestTwoParametersBothForbidden()
    {
        const string code = """
            public sealed class Tester
            {
                private const string _constValue = "foo";
                private static readonly string StaticValue = "bar";

                public void Test()
                {
                    TestClass.TwoParametersBothForbidden(_constValue, _constValue);
                    TestClass.TwoParametersBothForbidden(_constValue, StaticValue);
                    TestClass.TwoParametersBothForbidden(_constValue, "whatever");
                    TestClass.TwoParametersBothForbidden("whatever", _constValue);
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(10,59): error RA0033: The "second" parameter of TwoParametersBothForbidden forbids literal values
            VerifyCS.Diagnostic().WithSpan(10, 59, 10, 69).WithArguments("second", "TwoParametersBothForbidden"),
            // /0/Test0.cs(11,46): error RA0033: The "first" parameter of TwoParametersBothForbidden forbids literal values
            VerifyCS.Diagnostic().WithSpan(11, 46, 11, 56).WithArguments("first", "TwoParametersBothForbidden")
        );
    }

    [Test]
    public async Task TestListParameter()
    {
        const string code = """
            public sealed class Tester
            {
                private const string _constValue = "foo";
                private static readonly string StaticValue = "bar";
                private static readonly StringWrapper WrappedValue = new("biz");

                public void Test()
                {
                    TestClass.ListParameterForbidden([_constValue, StaticValue, WrappedValue]);
                    TestClass.ListParameterForbidden(["foo", _constValue, "bar"]);
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(10,43): warning RA0033: The "values" parameter of ListParameterForbidden forbids literal values
            VerifyCS.Diagnostic().WithSpan(10, 43, 10, 48).WithArguments("values", "ListParameterForbidden"),
            // /0/Test0.cs(10,63): warning RA0033: The "values" parameter of ListParameterForbidden forbids literal values
            VerifyCS.Diagnostic().WithSpan(10, 63, 10, 68).WithArguments("values", "ListParameterForbidden")
        );
    }

    [Test]
    public async Task TestParamsListParameter()
    {
        const string code = """
            public sealed class Tester
            {
                private const string _constValue = "foo";
                private static readonly string StaticValue = "bar";
                private static readonly StringWrapper WrappedValue = new("biz");

                public void Test()
                {
                    TestClass.ParamsListParameterForbidden(_constValue, StaticValue, WrappedValue);
                    TestClass.ParamsListParameterForbidden("foo", _constValue, "bar");
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(10,48): warning RA0033: The "values" parameter of ParamsListParameterForbidden forbids literal values
            VerifyCS.Diagnostic().WithSpan(10, 48, 10, 53).WithArguments("values", "ParamsListParameterForbidden"),
            // /0/Test0.cs(10,68): warning RA0033: The "values" parameter of ParamsListParameterForbidden forbids literal values
            VerifyCS.Diagnostic().WithSpan(10, 68, 10, 73).WithArguments("values", "ParamsListParameterForbidden")
        );
    }
}
