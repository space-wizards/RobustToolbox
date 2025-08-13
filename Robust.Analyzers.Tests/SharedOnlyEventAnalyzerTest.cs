using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.SharedOnlyEventAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
[TestOf(typeof(SharedOnlyEventAnalyzer))]
public sealed class SharedOnlyEventAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new RTAnalyzerTest<SharedOnlyEventAnalyzer>()
        {
            TestState =
            {
                Sources = { code }
            },
        };

        TestHelper.AddEmbeddedSources(
            test.TestState,
            "Robust.Shared.Analyzers.SharedOnlyEventAttribute.cs"
        );

        test.TestState.Sources.Add(("TestTypeDefs.cs", TestTypeDefs));

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    private const string TestTypeDefs = """
        using Robust.Shared.Analyzers;

        public sealed class DummyEventBus
        {
            public void SubscribeLocalEvent<TComp, TEvent>() { }
        }

        public sealed class TestComponent;

        [SharedOnlyEvent]
        public record struct SharedEvent;
        public record struct UnsharedEvent;
        [SharedOnlyEvent(false)]
        public record struct SharedNoClientEvent;
    """;

    [Test]
    public async Task ServerSubscribeTest()
    {
        const string code = """
            namespace Content.Server.Test
            {
                public sealed class Tester
                {
                    private DummyEventBus _bus = new();

                    public void Test()
                    {
                        _bus.SubscribeLocalEvent<TestComponent, SharedEvent>();
                        _bus.SubscribeLocalEvent<TestComponent, UnsharedEvent>();
                        _bus.SubscribeLocalEvent<TestComponent, SharedNoClientEvent>();
                    }
                }
            }
        """;

        await Verifier(code,
            // /0/Test0.cs(9,17): warning RA0040: The event SharedEvent should only be subscribed to in Shared
            VerifyCS.Diagnostic().WithSpan(9, 17, 9, 71).WithArguments("SharedEvent"),
            // /0/Test0.cs(11,17): warning RA0040: The event SharedNoClientEvent should only be subscribed to in Shared
            VerifyCS.Diagnostic().WithSpan(11, 17, 11, 79).WithArguments("SharedNoClientEvent")
        );
    }

    [Test]
    public async Task SharedSubscribeTest()
    {
        const string code = """
            namespace Content.Shared.Test
            {
                public sealed class Tester
                {
                    private DummyEventBus _bus = new();

                    public void Test()
                    {
                        _bus.SubscribeLocalEvent<TestComponent, SharedEvent>();
                        _bus.SubscribeLocalEvent<TestComponent, UnsharedEvent>();
                        _bus.SubscribeLocalEvent<TestComponent, SharedNoClientEvent>();
                    }
                }
            }
        """;

        await Verifier(code, []);
    }

    [Test]
    public async Task ClientSubscribeTest()
    {
        const string code = """
            namespace Content.Client.Test
            {
                public sealed class Tester
                {
                    private DummyEventBus _bus = new();

                    public void Test()
                    {
                        _bus.SubscribeLocalEvent<TestComponent, SharedEvent>();
                        _bus.SubscribeLocalEvent<TestComponent, UnsharedEvent>();
                        _bus.SubscribeLocalEvent<TestComponent, SharedNoClientEvent>();
                    }
                }
            }
        """;

        await Verifier(code,
            // /0/Test0.cs(11,17): warning RA0040: The event SharedNoClientEvent should only be subscribed to in Shared
            VerifyCS.Diagnostic().WithSpan(11, 17, 11, 79).WithArguments("SharedNoClientEvent")
        );
    }
}
