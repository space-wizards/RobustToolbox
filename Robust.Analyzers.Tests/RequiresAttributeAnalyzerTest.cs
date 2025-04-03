using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.RequiresAttributeAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
public sealed class RequiresAttributeAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<RequiresAttributeAnalyzer, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { code },
            },
        };

        test.TestState.Sources.Add(("TestTypeDefs.cs", TestTypeDefs));

        TestHelper.AddEmbeddedSources(
            test.TestState,
            "Robust.Shared.Analyzers.RequiresAttributeAttribute.cs"
        );

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    private const string TestTypeDefs = """
        using System;
        using Robust.Shared.Analyzers;

        public sealed class FirstAttributeAttribute : Attribute;
        public sealed class SecondAttributeAttribute : Attribute;
        [AttributeUsage(AttributeTargets.All, Inherited = false)]
        public sealed class NotInheritedAttributeAttribute : Attribute;

        public interface ISharedInterface;

        public sealed class WithNoAttribute : ISharedInterface;
        [FirstAttribute]
        public sealed class WithFirstAttribute : ISharedInterface;
        [SecondAttribute]
        public sealed class WithSecondAttribute : ISharedInterface;
        [FirstAttribute, SecondAttribute]
        public sealed class WithBothAttributes : ISharedInterface;
        [NotInheritedAttribute]
        public sealed class WithNotInheritedAttribute : ISharedInterface;

        [FirstAttribute]
        public abstract class WithFirstAttributeAbstract : ISharedInterface;
        [SecondAttribute]
        public abstract class WithSecondAttributeAbstract : ISharedInterface;
        [NotInheritedAttribute]
        public abstract class WithNotInheritedAttributeAbstract : ISharedInterface;

        public sealed class WithFirstAttributeInherited : WithFirstAttributeAbstract;
        public sealed class WithSecondAttributeInherited : WithSecondAttributeAbstract;
        public sealed class WithNotInheritedAttributeInherited : WithNotInheritedAttributeAbstract;

        public record struct Wrapper<T>(T value) where T : ISharedInterface
        {
            public T Value = value;
        }

        public record struct Wrapper<T1, T2>(T1 value1, T2 value2)
            where T1 : ISharedInterface
            where T2 : ISharedInterface
        {
            public T1 Value1 = value1;
            public T2 Value2 = value2;
        }

        public sealed class TestClass
        {
            public static void RequireFirst([RequiresAttribute(typeof(FirstAttributeAttribute))] ISharedInterface value) { }
            public static void RequireBoth([RequiresAttribute(typeof(FirstAttributeAttribute)), RequiresAttribute(typeof(SecondAttributeAttribute))] ISharedInterface value) { }
            public static void RequireSeparate([RequiresAttribute(typeof(FirstAttributeAttribute))] ISharedInterface first, [RequiresAttribute(typeof(SecondAttributeAttribute))] ISharedInterface second) { }
            public static void RequireNotInherited([RequiresAttribute(typeof(NotInheritedAttributeAttribute))] ISharedInterface value) { }

            public static void RequireFirstGeneric<T>([RequiresAttribute(typeof(FirstAttributeAttribute))] T value) where T : ISharedInterface { }
            public static void RequireBothGeneric<T>([RequiresAttribute(typeof(FirstAttributeAttribute)), RequiresAttribute(typeof(SecondAttributeAttribute))] T value) where T : ISharedInterface { }

            public static void RequireFirstInWrapped<[RequiresAttribute(typeof(FirstAttributeAttribute))] T>(Wrapper<T> wrapped) where T : ISharedInterface { }
            public static void RequireBothInWrapped<[RequiresAttribute(typeof(FirstAttributeAttribute)), RequiresAttribute(typeof(SecondAttributeAttribute))] T>(Wrapper<T> wrapped) where T : ISharedInterface { }
            public static void RequireFirstSecondInTwinWrapped<[RequiresAttribute(typeof(FirstAttributeAttribute))] T1, [RequiresAttribute(typeof(SecondAttributeAttribute))] T2>(Wrapper<T1, T2> wrapped)
                where T1 : ISharedInterface
                where T2 : ISharedInterface { }

            public static WithNoAttribute WithNo = new();
            public static WithFirstAttribute WithFirst = new();
            public static WithSecondAttribute WithSecond = new();
            public static WithBothAttributes WithBoth = new();
            public static WithNotInheritedAttribute WithNotInherited = new();

            public static WithFirstAttributeInherited WithFirstInherited = new();
            public static WithSecondAttributeInherited WithSecondInherited = new();
            public static WithNotInheritedAttributeInherited WithNotInheritedInherited = new();

            public static Wrapper<WithNoAttribute> WithNoWrapped = new();
            public static Wrapper<WithFirstAttribute> WithFirstWrapped = new();
            public static Wrapper<WithSecondAttribute> WithSecondWrapped = new();
            public static Wrapper<WithBothAttributes> WithBothWrapped = new();

            public static Wrapper<WithFirstAttribute, WithFirstAttribute> WithFirstFirstWrapped = new();
            public static Wrapper<WithNoAttribute, WithNoAttribute> WithNoNoWrapped = new();
            public static Wrapper<WithFirstAttribute, WithNoAttribute> WithFirstNoWrapped = new();
            public static Wrapper<WithFirstAttribute, WithSecondAttribute> WithFirstSecondWrapped = new();
        }
    """;

    [Test]
    public async Task TestWithOneAttribute()
    {
        const string code = """
            public sealed class Tester
            {
                public void Test()
                {
                    TestClass.RequireFirst(TestClass.WithNo); // Bad
                    TestClass.RequireFirst(TestClass.WithFirst);
                    TestClass.RequireFirst(TestClass.WithSecond); // Bad
                    TestClass.RequireFirst(TestClass.WithBoth);
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(5,32): error RA0037: Type (WithNoAttribute) of passed value is missing required attribute FirstAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeMissingAttributeRule).WithSpan(5, 32, 5, 48).WithArguments("WithNoAttribute", "FirstAttributeAttribute"),
            // /0/Test0.cs(7,32): error RA0037: Type (WithSecondAttribute) of passed value is missing required attribute FirstAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeMissingAttributeRule).WithSpan(7, 32, 7, 52).WithArguments("WithSecondAttribute", "FirstAttributeAttribute")
        );
    }

    [Test]
    public async Task TestWithTwoAttributes()
    {
        const string code = """
            public sealed class Tester
            {
                public void Test()
                {
                    TestClass.RequireBoth(TestClass.WithNo); // Bad x2
                    TestClass.RequireBoth(TestClass.WithFirst); // Bad
                    TestClass.RequireBoth(TestClass.WithSecond); // Bad
                    TestClass.RequireBoth(TestClass.WithBoth);
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(5,31): error RA0037: Type (WithNoAttribute) of passed value is missing required attribute FirstAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeMissingAttributeRule).WithSpan(5, 31, 5, 47).WithArguments("WithNoAttribute", "FirstAttributeAttribute"),
            // /0/Test0.cs(5,31): error RA0037: Type (WithNoAttribute) of passed value is missing required attribute SecondAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeMissingAttributeRule).WithSpan(5, 31, 5, 47).WithArguments("WithNoAttribute", "SecondAttributeAttribute"),
            // /0/Test0.cs(6,31): error RA0037: Type (WithFirstAttribute) of passed value is missing required attribute SecondAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeMissingAttributeRule).WithSpan(6, 31, 6, 50).WithArguments("WithFirstAttribute", "SecondAttributeAttribute"),
            // /0/Test0.cs(7,31): error RA0037: Type (WithSecondAttribute) of passed value is missing required attribute FirstAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeMissingAttributeRule).WithSpan(7, 31, 7, 51).WithArguments("WithSecondAttribute", "FirstAttributeAttribute")
        );
    }

    [Test]
    public async Task TestWithSeparateAttributes()
    {
        const string code = """
            public sealed class Tester
            {
                public void Test()
                {
                    TestClass.RequireSeparate(TestClass.WithNo, TestClass.WithNo); // Bad x2
                    TestClass.RequireSeparate(TestClass.WithFirst, TestClass.WithSecond);
                    TestClass.RequireSeparate(TestClass.WithSecond, TestClass.WithFirst); // Bad x2
                    TestClass.RequireSeparate(TestClass.WithFirst, TestClass.WithFirst); // Bad
                    TestClass.RequireSeparate(TestClass.WithSecond, TestClass.WithSecond); // Bad
                    TestClass.RequireSeparate(TestClass.WithBoth, TestClass.WithBoth);
                    TestClass.RequireSeparate(TestClass.WithFirst, TestClass.WithBoth);
                    TestClass.RequireSeparate(TestClass.WithBoth, TestClass.WithSecond);
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(5,35): error RA0037: Type (WithNoAttribute) of passed value is missing required attribute FirstAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeMissingAttributeRule).WithSpan(5, 35, 5, 51).WithArguments("WithNoAttribute", "FirstAttributeAttribute"),
            // /0/Test0.cs(5,53): error RA0037: Type (WithNoAttribute) of passed value is missing required attribute SecondAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeMissingAttributeRule).WithSpan(5, 53, 5, 69).WithArguments("WithNoAttribute", "SecondAttributeAttribute"),
            // /0/Test0.cs(7,35): error RA0037: Type (WithSecondAttribute) of passed value is missing required attribute FirstAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeMissingAttributeRule).WithSpan(7, 35, 7, 55).WithArguments("WithSecondAttribute", "FirstAttributeAttribute"),
            // /0/Test0.cs(7,57): error RA0037: Type (WithFirstAttribute) of passed value is missing required attribute SecondAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeMissingAttributeRule).WithSpan(7, 57, 7, 76).WithArguments("WithFirstAttribute", "SecondAttributeAttribute"),
            // /0/Test0.cs(8,56): error RA0037: Type (WithFirstAttribute) of passed value is missing required attribute SecondAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeMissingAttributeRule).WithSpan(8, 56, 8, 75).WithArguments("WithFirstAttribute", "SecondAttributeAttribute"),
            // /0/Test0.cs(9,35): error RA0037: Type (WithSecondAttribute) of passed value is missing required attribute FirstAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeMissingAttributeRule).WithSpan(9, 35, 9, 55).WithArguments("WithSecondAttribute", "FirstAttributeAttribute")
        );
    }

    [Test]
    public async Task TestProxyWithOneAttribute()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            public sealed class Tester
            {
                public static void ProxyRequireFirst([RequiresAttribute(typeof(FirstAttributeAttribute))] ISharedInterface value)
                {
                    TestClass.RequireFirst(value);
                }

                public void Test()
                {
                    ProxyRequireFirst(TestClass.WithNo); // Bad
                    ProxyRequireFirst(TestClass.WithFirst);
                    ProxyRequireFirst(TestClass.WithSecond); // Bad
                    ProxyRequireFirst(TestClass.WithBoth);
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(12,27): error RA0037: Type (WithNoAttribute) of passed value is missing required attribute FirstAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeMissingAttributeRule).WithSpan(12, 27, 12, 43).WithArguments("WithNoAttribute", "FirstAttributeAttribute"),
            // /0/Test0.cs(14,27): error RA0037: Type (WithSecondAttribute) of passed value is missing required attribute FirstAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeMissingAttributeRule).WithSpan(14, 27, 14, 47).WithArguments("WithSecondAttribute", "FirstAttributeAttribute")
        );
    }

    [Test]
    public async Task TestProxyWrapperWithOneAttribute()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            public sealed class Tester
            {
                public static void ProxyRequireFirstInWrapped<[RequiresAttribute(typeof(FirstAttributeAttribute))]T>(Wrapper<T> wrapped) where T : ISharedInterface
                {
                    TestClass.RequireFirstInWrapped<T>(wrapped);
                }

                public void Test()
                {
                    ProxyRequireFirstInWrapped(TestClass.WithNoWrapped); // Bad
                    ProxyRequireFirstInWrapped(TestClass.WithFirstWrapped);
                    ProxyRequireFirstInWrapped(TestClass.WithSecondWrapped); // Bad
                    ProxyRequireFirstInWrapped(TestClass.WithBothWrapped);
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(12,36): error RA0038: Type argument T of passed value is type WithNoAttribute which is missing required attribute FirstAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeTypeArgMissingAttributeRule).WithSpan(12, 36, 12, 59).WithArguments("T", "WithNoAttribute", "FirstAttributeAttribute"),
            // /0/Test0.cs(14,36): error RA0038: Type argument T of passed value is type WithSecondAttribute which is missing required attribute FirstAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeTypeArgMissingAttributeRule).WithSpan(14, 36, 14, 63).WithArguments("T", "WithSecondAttribute", "FirstAttributeAttribute")
        );
    }

    [Test]
    public async Task TestGenericWithOneAttribute()
    {
        const string code = """
            public sealed class Tester
            {
                public void Test()
                {
                    TestClass.RequireFirstGeneric(TestClass.WithNo); // Bad
                    TestClass.RequireFirstGeneric(TestClass.WithFirst);
                    TestClass.RequireFirstGeneric(TestClass.WithSecond); // Bad
                    TestClass.RequireFirstGeneric(TestClass.WithBoth);
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(5,39): error RA0037: Type (WithNoAttribute) of passed value is missing required attribute FirstAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeMissingAttributeRule).WithSpan(5, 39, 5, 55).WithArguments("WithNoAttribute", "FirstAttributeAttribute"),
            // /0/Test0.cs(7,39): error RA0037: Type (WithSecondAttribute) of passed value is missing required attribute FirstAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeMissingAttributeRule).WithSpan(7, 39, 7, 59).WithArguments("WithSecondAttribute", "FirstAttributeAttribute")
        );
    }

    [Test]
    public async Task TestWrapperWithOneAttribute()
    {
        const string code = """
            public sealed class Tester
            {
                public void Test()
                {
                    TestClass.RequireFirstInWrapped(TestClass.WithNoWrapped); // Bad
                    TestClass.RequireFirstInWrapped(TestClass.WithFirstWrapped);
                    TestClass.RequireFirstInWrapped(TestClass.WithSecondWrapped); // Bad
                    TestClass.RequireFirstInWrapped(TestClass.WithBothWrapped);
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(5,41): error RA0038: Type argument T of passed value is type WithNoAttribute which is missing required attribute FirstAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeTypeArgMissingAttributeRule).WithSpan(5, 41, 5, 64).WithArguments("T", "WithNoAttribute", "FirstAttributeAttribute"),
            // /0/Test0.cs(7,41): error RA0038: Type argument T of passed value is type WithSecondAttribute which is missing required attribute FirstAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeTypeArgMissingAttributeRule).WithSpan(7, 41, 7, 68).WithArguments("T", "WithSecondAttribute", "FirstAttributeAttribute")
        );
    }

    [Test]
    public async Task TestTwinWrapper()
    {
        const string code = """
            public sealed class Tester
            {
                public void Test()
                {
                    TestClass.RequireFirstSecondInTwinWrapped(TestClass.WithNoNoWrapped); // Bad x2
                    TestClass.RequireFirstSecondInTwinWrapped(TestClass.WithFirstNoWrapped); // Bad
                    TestClass.RequireFirstSecondInTwinWrapped(TestClass.WithFirstFirstWrapped); // Bad
                    TestClass.RequireFirstSecondInTwinWrapped(TestClass.WithFirstSecondWrapped);
                }
            }
            """;

        await Verifier(code,
           // /0/Test0.cs(5,51): error RA0038: Type argument T1 of passed value is type WithNoAttribute which is missing required attribute FirstAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeTypeArgMissingAttributeRule).WithSpan(5, 51, 5, 76).WithArguments("T1", "WithNoAttribute", "FirstAttributeAttribute"),
            // /0/Test0.cs(5,51): error RA0038: Type argument T2 of passed value is type WithNoAttribute which is missing required attribute SecondAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeTypeArgMissingAttributeRule).WithSpan(5, 51, 5, 76).WithArguments("T2", "WithNoAttribute", "SecondAttributeAttribute"),
            // /0/Test0.cs(6,51): error RA0038: Type argument T2 of passed value is type WithNoAttribute which is missing required attribute SecondAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeTypeArgMissingAttributeRule).WithSpan(6, 51, 6, 79).WithArguments("T2", "WithNoAttribute", "SecondAttributeAttribute"),
            // /0/Test0.cs(7,51): error RA0038: Type argument T2 of passed value is type WithFirstAttribute which is missing required attribute SecondAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeTypeArgMissingAttributeRule).WithSpan(7, 51, 7, 82).WithArguments("T2", "WithFirstAttribute", "SecondAttributeAttribute")
        );
    }

    [Test]
    public async Task TestWithOneAttributeInherited()
    {
        const string code = """
            public sealed class Tester
            {
                public void Test()
                {
                    TestClass.RequireFirst(TestClass.WithFirstInherited);
                    TestClass.RequireFirst(TestClass.WithSecondInherited); // Bad
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(6,32): error RA0037: Type (WithSecondAttributeInherited) of passed value is missing required attribute FirstAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeMissingAttributeRule).WithSpan(6, 32, 6, 61).WithArguments("WithSecondAttributeInherited", "FirstAttributeAttribute")
        );
    }

    [Test]
    public async Task TestWithOneAttributeNotInherited()
    {
        const string code = """
            public sealed class Tester
            {
                public void Test()
                {
                    TestClass.RequireNotInherited(TestClass.WithNotInherited);
                    TestClass.RequireNotInherited(TestClass.WithNotInheritedInherited); // Bad
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(6,39): error RA0037: Type (WithNotInheritedAttributeInherited) of passed value is missing required attribute NotInheritedAttributeAttribute
            VerifyCS.Diagnostic(RequiresAttributeAnalyzer.RequiresAttributeMissingAttributeRule).WithSpan(6, 39, 6, 74).WithArguments("WithNotInheritedAttributeInherited", "NotInheritedAttributeAttribute")
        );
    }
}