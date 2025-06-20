using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.ProxyForAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

public sealed class ProxyForAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<ProxyForAnalyzer, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { code },
            },
        };

        TestHelper.AddEmbeddedSources(
            test.TestState,
            "Robust.Shared.Analyzers.ProxyForAttribute.cs"
        );

        test.TestState.Sources.Add(("TestTypeDefs.cs", TestTypeDefs));

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    private const string TestTypeDefs = """
        using Robust.Shared.Analyzers;

        public sealed class TargetClass
        {
            public void DoSomething() { }
            public bool TryDoSomething<T>(int foo, out T? bar)
            {
                bar = default;
                return true;
            }
        }

        public abstract partial class ProxyClass
        {
            protected TargetClass TargetClass = new();
        }
    """;

    [Test]
    public async Task TestAutoName()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            public abstract partial class ProxyClass
            {
                [ProxyFor(typeof(TargetClass))]
                public void DoSomething()
                {
                    TargetClass.DoSomething();
                }
            }

            public sealed class Tester : ProxyClass
            {
                public void Good()
                {
                    DoSomething();
                }

                public void Bad()
                {
                    TargetClass.DoSomething();
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(18,9): warning RA0037: Use the proxy method DoSomething instead of calling TargetClass.DoSomething directly
            VerifyCS.Diagnostic(ProxyForAnalyzer.PreferProxyDescriptor).WithSpan(21, 9, 21, 34).WithArguments("DoSomething", "TargetClass.DoSomething")
        );
    }

    [Test]
    public async Task TestSetName()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            public abstract partial class ProxyClass
            {
                [ProxyFor(typeof(TargetClass), nameof(TargetClass.DoSomething))]
                public void DoIt()
                {
                    TargetClass.DoSomething();
                }
            }

            public sealed class Tester : ProxyClass
            {
                public void Good()
                {
                    DoIt();
                }

                public void Bad()
                {
                    TargetClass.DoSomething();
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(21,9): warning RA0037: Use the proxy method DoIt instead of calling TargetClass.DoSomething directly
            VerifyCS.Diagnostic(ProxyForAnalyzer.PreferProxyDescriptor).WithSpan(21, 9, 21, 34).WithArguments("DoIt", "TargetClass.DoSomething")
        );
    }

    [Test]
    public async Task TestGeneric()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            public abstract partial class ProxyClass
            {
                [ProxyFor(typeof(TargetClass))]
                public bool TryDoSomething<T>(int foo, out T? bar)
                {
                    return TargetClass.TryDoSomething(foo, out bar);
                }
            }

            public sealed class Tester : ProxyClass
            {
                public void Good()
                {
                    TryDoSomething<string>(5, out var bar);
                }

                public void Bad()
                {
                    TargetClass.TryDoSomething<string>(5, out var bar);
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(21,9): warning RA0037: Use the proxy method TryDoSomething instead of calling TargetClass.TryDoSomething directly
            VerifyCS.Diagnostic(ProxyForAnalyzer.PreferProxyDescriptor).WithSpan(21, 9, 21, 59).WithArguments("TryDoSomething", "TargetClass.TryDoSomething")
        );
    }

    [Test]
    public async Task TestRedundantMethodName()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            public abstract partial class ProxyClass
            {
                [ProxyFor(typeof(TargetClass), nameof(TargetClass.DoSomething))]
                public void DoSomething()
                {
                    TargetClass.DoSomething();
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(5,36): warning RA0038: Set method name matches the proxy method name and can be omitted
            VerifyCS.Diagnostic(ProxyForAnalyzer.RedundantMethodNameDescriptor).WithSpan(5, 36, 5, 67)
        );
    }

    [Test]
    public async Task TestNoMatchingSetMethodName()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            public abstract partial class ProxyClass
            {
                [ProxyFor(typeof(TargetClass), "SomeOtherName")]
                public void DoSomething()
                {
                    TargetClass.DoSomething();
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(5,15): error RA0039: Unable to find target method TargetClass.SomeOtherName()
            VerifyCS.Diagnostic(ProxyForAnalyzer.TargetMethodNotFoundDescriptor).WithSpan(5, 15, 5, 34).WithArguments("TargetClass.SomeOtherName()")
        );
    }

    [Test]
    public async Task TestNoMatchingSignature()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            public abstract partial class ProxyClass
            {
                [ProxyFor(typeof(TargetClass))]
                public void DoSomething(int foo)
                {
                    TargetClass.DoSomething();
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(5,15): error RA0039: Unable to find target method TargetClass.DoSomething(int foo)
            VerifyCS.Diagnostic(ProxyForAnalyzer.TargetMethodNotFoundDescriptor).WithSpan(5, 15, 5, 34).WithArguments("TargetClass.DoSomething(int foo)")
        );
    }

    [Test]
    public async Task TestIgnoreDelegate()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            public abstract partial class ProxyClass
            {
                [ProxyFor(typeof(TargetClass))]
                public void DoSomething()
                {
                    TargetClass.DoSomething();
                }

                public delegate void ThingDoer(TargetClass TargetClass);

                public void RunDelegate(ThingDoer doer) { }
            }

            public sealed class Tester : ProxyClass
            {
                public void Test()
                {
                    RunDelegate(target =>
                    {
                        target.DoSomething();
                    });
                }
            }
            """;

        await Verifier(code, []);
    }

    [Test]
    public async Task TestNoMatchingAutoMethodName()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            public abstract partial class ProxyClass
            {
                [ProxyFor(typeof(TargetClass))]
                public void SomeOtherName()
                {
                    TargetClass.DoSomething();
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(5,15): error RA0039: Unable to find target method TargetClass.SomeOtherName()
            VerifyCS.Diagnostic(ProxyForAnalyzer.TargetMethodNotFoundDescriptor).WithSpan(5, 15, 5, 34).WithArguments("TargetClass.SomeOtherName()")
        );
    }
}
