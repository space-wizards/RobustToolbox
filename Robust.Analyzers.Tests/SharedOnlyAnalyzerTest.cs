using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.SharedOnlyAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
[TestOf(typeof(SharedOnlyAnalyzer))]
public sealed class SharedOnlyAnalyzerTest
{
    private static Task Verifier(string code, string assemblyName, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<SharedOnlyAnalyzer, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { code },
            },
        };

        TestHelper.AddEmbeddedSources(
            test.TestState,
            "Robust.Shared.Analyzers.SharedOnlyAttribute.cs"
        );

        test.TestState.Sources.Add(("TestTypeDefs.cs", TestTypeDefs));

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            // Since this analyzer works based on the assembly name,
            // we need to override it per-test to make the test work
            return solution.WithProjectAssemblyName(projectId, assemblyName);
        });

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    private const string TestTypeDefs = """
        using Robust.Shared.Analyzers;

        namespace Robust.Shared.Testing
        {
            public sealed class Foo
            {
                // These types would realistically always be in Shared,
                // but we only get one test assembly, so we just ignore the warning
                #pragma warning disable RA0048
                [SharedOnly]
                public void SharedMethod() { }
                [SharedOnly]
                public bool SharedProperty { get; set; }
                [SharedOnly]
                public bool SharedField;
                #pragma warning restore RA0048
            }
        }
    """;

    [Test]
    [Description("Tests that using a [SharedOnly] method/property/field from Client code raises a warning")]
    public async Task TestClient()
    {
        const string code = """
            using Robust.Shared.Testing;

            public sealed class ClientTester
            {
                public void Test()
                {
                    var foo = new Foo();

                    foo.SharedMethod();
                    var x = foo.SharedProperty;
                    var y = foo.SharedField;
                }
            }
            """;

        await Verifier(code, "Robust.Client.Testing",
            // /0/Test0.cs(11,13): warning RA0048: SharedMethod should only be used in Shared assemblies
            VerifyCS.Diagnostic().WithSpan(9, 9, 9, 27).WithArguments("SharedMethod"),
            // /0/Test0.cs(10,21): warning RA0048: SharedProperty should only be used in Shared assemblies
            VerifyCS.Diagnostic().WithSpan(10, 17, 10, 35).WithArguments("SharedProperty"),
            // /0/Test0.cs(11,21): warning RA0048: SharedField should only be used in Shared assemblies
            VerifyCS.Diagnostic().WithSpan(11, 17, 11, 32).WithArguments("SharedField")
        );
    }

    [Test]
    [Description("Tests that using a [SharedOnly] method/property/field from Server code raises a warning")]
    public async Task TestServer()
    {
        const string code = """
            using Robust.Shared.Testing;

            public sealed class ServerTester
            {
                public void Test()
                {
                    var foo = new Foo();

                    foo.SharedMethod();
                    var x = foo.SharedProperty;
                    var y = foo.SharedField;
                }
            }
            """;

        await Verifier(code, "Robust.Server.Testing",
            // /0/Test0.cs(9,9): warning RA0048: SharedMethod should only be used in Shared assemblies
            VerifyCS.Diagnostic().WithSpan(9, 9, 9, 27).WithArguments("SharedMethod"),
            // /0/Test0.cs(10,17): warning RA0048: SharedProperty should only be used in Shared assemblies
            VerifyCS.Diagnostic().WithSpan(10, 17, 10, 35).WithArguments("SharedProperty"),
            // /0/Test0.cs(11,17): warning RA0048: SharedField should only be used in Shared assemblies
            VerifyCS.Diagnostic().WithSpan(11, 17, 11, 32).WithArguments("SharedField")
        );
    }

    [Test]
    [Description("Tests that using a [SharedOnly] method/property/field from Shared code does not raise a warning")]
    public async Task TestShared()
    {
        const string code = """
            using Robust.Shared.Testing;

            public sealed class SharedTester
            {
                public void Test()
                {
                    var foo = new Foo();

                    foo.SharedMethod();
                    var x = foo.SharedProperty;
                    var y = foo.SharedField;
                }
            }
            """;

        await Verifier(code, "Robust.Shared.Testing", []);
    }

    [Test]
    [Description("Tests that marking a Client method/property/field as [SharedOnly] raises a warning")]
    public async Task TestAttribute()
    {
        const string code = """
            using Robust.Shared.Analyzers;

            public sealed class Foo
            {
                [SharedOnly]
                public void DoThing() { }
                [SharedOnly]
                public bool Bar { get; set; }
                [SharedOnly]
                public bool Baz;
            }
            """;

        await Verifier(code, "Robust.Client.Testing",
            // /0/Test0.cs(5,6): warning RA0048: SharedOnlyAttribute should only be used in Shared assemblies
            VerifyCS.Diagnostic().WithSpan(5, 6, 5, 16).WithArguments("SharedOnlyAttribute"),
            // /0/Test0.cs(7,6): warning RA0048: SharedOnlyAttribute should only be used in Shared assemblies
            VerifyCS.Diagnostic().WithSpan(7, 6, 7, 16).WithArguments("SharedOnlyAttribute"),
            // /0/Test0.cs(9,6): warning RA0048: SharedOnlyAttribute should only be used in Shared assemblies
            VerifyCS.Diagnostic().WithSpan(9, 6, 9, 16).WithArguments("SharedOnlyAttribute")
        );
    }
}
