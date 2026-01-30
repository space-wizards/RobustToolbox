using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.ProxyForAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
[TestOf(typeof(ProxyForFixer))]
public sealed class ProxyForFixerTest
{
    private static Task Verifier(string code, string fixedCode, params DiagnosticResult[] expected)
    {
        var test = new CSharpCodeFixTest<ProxyForAnalyzer, ProxyForFixer, DefaultVerifier>()
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
            "Robust.Shared.Analyzers.ProxyForAttribute.cs"
        );

        TestHelper.AddEmbeddedSources(
            test.FixedState,
            "Robust.Shared.Analyzers.ProxyForAttribute.cs"
        );

        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    [Test]
    public async Task TestSubstituteProxy()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            public sealed class TargetClass
            {
                public void DoSomething<T>(T? foo, string bar) { }
            }

            public abstract class ProxyClass
            {
                protected TargetClass TargetClass = new();

                [ProxyFor(typeof(TargetClass))]
                public void DoSomething<T>(T? foo, string bar)
                {
                    TargetClass.DoSomething(foo, bar);
                }
            }

            public sealed class Tester : ProxyClass
            {
                public void Test()
                {
                    // Comment

                    TargetClass.DoSomething<int>(5, "bar");

                }
            }
            """;

        const string fixedCode = """
            using Robust.Shared.Analyzers;

            public sealed class TargetClass
            {
                public void DoSomething<T>(T? foo, string bar) { }
            }

            public abstract class ProxyClass
            {
                protected TargetClass TargetClass = new();

                [ProxyFor(typeof(TargetClass))]
                public void DoSomething<T>(T? foo, string bar)
                {
                    TargetClass.DoSomething(foo, bar);
                }
            }

            public sealed class Tester : ProxyClass
            {
                public void Test()
                {
                    // Comment

                    DoSomething<int>(5, "bar");

                }
            }
            """;

        await Verifier(code, fixedCode,
            // /0/Test0.cs(23,9): warning RA0037: Use the proxy method DoSomething instead of calling TargetClass.DoSomething directly
            VerifyCS.Diagnostic(ProxyForAnalyzer.PreferProxyDescriptor).WithSpan(25, 9, 25, 47).WithArguments("DoSomething", "TargetClass.DoSomething")
        );
    }

    [Test]
    public async Task TestRemoveRedundantMethodName()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            public sealed class TargetClass
            {
                public void DoSomething(int foo, string bar) { }
            }

            public abstract class ProxyClass
            {
                protected TargetClass TargetClass = new();

                [ProxyFor(typeof(TargetClass), nameof(TargetClass.DoSomething))]
                public void DoSomething(int foo, string bar)
                {
                    TargetClass.DoSomething(foo, bar);
                }
            }
            """;

        const string fixedCode = """
            using Robust.Shared.Analyzers;

            public sealed class TargetClass
            {
                public void DoSomething(int foo, string bar) { }
            }

            public abstract class ProxyClass
            {
                protected TargetClass TargetClass = new();

                [ProxyFor(typeof(TargetClass))]
                public void DoSomething(int foo, string bar)
                {
                    TargetClass.DoSomething(foo, bar);
                }
            }
            """;

        await Verifier(code, fixedCode,
            // /0/Test0.cs(12,36): warning RA0038: Set method name matches the proxy method name and can be omitted
            VerifyCS.Diagnostic(ProxyForAnalyzer.RedundantMethodNameDescriptor).WithSpan(12, 36, 12, 67)
        );
    }
}
