using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.ByRefEventAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture, TestOf(typeof(ByRefEventAnalyzer))]
public sealed class ByRefEventAnalyzerTest
{
    private const string EventBusDef = """
        namespace Robust.Shared.GameObjects;

        public readonly struct EntityUid;

        public sealed class EntitySystem
        {
            public void RaiseLocalEvent<TEvent>(EntityUid uid, ref TEvent args, bool broadcast = false)
                where TEvent : notnull { }

            public void RaiseLocalEvent<TEvent>(EntityUid uid, TEvent args, bool broadcast = false)
                where TEvent : notnull { }
        }

        public sealed class EntityEventBus
        {
            public void RaiseLocalEvent<TEvent>(EntityUid uid, ref TEvent args, bool broadcast = false)
                where TEvent : notnull { }

            public void RaiseLocalEvent<TEvent>(EntityUid uid, TEvent args, bool broadcast = false)
                where TEvent : notnull { }
        }
        """;

    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<ByRefEventAnalyzer, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { code }
            },
        };

        TestHelper.AddEmbeddedSources(
            test.TestState,
            "Robust.Shared.GameObjects.EventBusAttributes.cs"
        );

        test.TestState.Sources.Add(("EntityEventBus.cs", EventBusDef));

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    [Test]
    public async Task TestSuccess()
    {
        const string code = """
            using Robust.Shared.GameObjects;

            [ByRefEvent]
            public readonly struct RefEvent;
            public readonly struct ValueEvent;

            public static class Foo
            {
                public static void Bar(EntityEventBus bus)
                {
                    bus.RaiseLocalEvent(default(EntityUid), new ValueEvent());
                    var refEv = new RefEvent();
                    bus.RaiseLocalEvent(default(EntityUid), ref refEv);
                }
            }
            """;

        await Verifier(code);
    }

    [Test]
    public async Task TestWrong()
    {
        const string code = """
            using Robust.Shared.GameObjects;

            [ByRefEvent]
            public readonly struct RefEvent;
            public readonly struct ValueEvent;

            public static class Foo
            {
                public static void Bar(EntityEventBus bus)
                {
                    bus.RaiseLocalEvent(default(EntityUid), new RefEvent());
                    var valueEv = new ValueEvent();
                    bus.RaiseLocalEvent(default(EntityUid), ref valueEv);
                }
            }
            """;

        await Verifier(
            code,
            // /0/Test0.cs(11,49): error RA0015: Tried to raise a by-ref event 'RefEvent' by value
            VerifyCS.Diagnostic(ByRefEventAnalyzer.ByRefEventRaisedByValueRule).WithSpan(11, 49, 11, 63).WithArguments("RefEvent"),
            // /0/Test0.cs(13,49): error RA0016: Tried to raise a value event 'ValueEvent' by-ref
            VerifyCS.Diagnostic(ByRefEventAnalyzer.ByValueEventRaisedByRefRule).WithSpan(13, 49, 13, 60).WithArguments("ValueEvent")
        );
    }
}
