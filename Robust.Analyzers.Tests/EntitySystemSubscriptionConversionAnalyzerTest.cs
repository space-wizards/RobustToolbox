using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.EntitySystemSubscriptionConversionAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[TestOf(typeof(EntitySystemSubscriptionConversionAnalyzer))]
public sealed class EntitySystemSubscriptionConversionAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<EntitySystemSubscriptionConversionAnalyzer, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { code }
            },
        };

        test.TestState.Sources.Add(("TestTypeDefs.cs", TestTypeDefs));

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    private const string TestTypeDefs = """
        using Robust.Shared.GameObjects;
        using System;

        namespace Robust.Shared.GameObjects
        {
            public interface IComponent;
            public abstract class Component : IComponent;

            public readonly struct EntityUid;

            public delegate void ComponentEventRefHandler<in TComp, TEvent>(EntityUid uid, TComp component, ref TEvent args)
                where TComp : IComponent
                where TEvent : notnull;

            public abstract class EntitySystem
            {
                public virtual void Initialize() { }
                public void SubscribeLocalEvent<TComp, TEvent>(
                    ComponentEventRefHandler<TComp, TEvent> handler,
                    Type[]? before = null,
                    Type[]? after = null)
                    where TComp : IComponent
                    where TEvent : notnull
                { }
            }
        }

        namespace Robust.Shared.Analyzers
        {
            public sealed class SubscribeLocalEventAttribute : Attribute;
        }

        public readonly struct TestEvent;
        public readonly struct TestEvent2;
        public readonly struct TestEvent3;
        public sealed partial class TestComponent : IComponent;
    """;

    [Test]
    [Description("Tests that a SubscribeLocalEvent invocation in an EntitySystem Intialize method is flagged as elligible for conversion.")]
    public async Task FlagSubscribeLocalEvent()
    {
        const string code = """
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();

                    SubscribeLocalEvent<TestComponent, TestEvent>(OnTest);
                }

                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(9,9): info RA0057: Initialize-based event subscription can be converted to attribute-based
            VerifyCS.Diagnostic().WithSpan(9, 9, 9, 62)
        );
    }

    [Test]
    [Description("Tests that subscriptions with before/after parameters are not flagged as elligible for conversion.")]
    // TODO: Remove this test if event subscription attributes get support for before/after parameters (and the code fixer is made to convert to them)
    public async Task IgnoreBeforeAfter()
    {
        const string code = """
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();

                    SubscribeLocalEvent<TestComponent, TestEvent>(OnTest, before: [typeof(Component)]);
                    SubscribeLocalEvent<TestComponent, TestEvent2>(OnTest2, after: [typeof(Component)]);
                    SubscribeLocalEvent<TestComponent, TestEvent3>(OnTest3, before: [typeof(Component)], after: [typeof(Component)]);
                }

                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args) { }
                private void OnTest2(EntityUid uid, TestComponent comp, ref TestEvent2 args) { }
                private void OnTest3(EntityUid uid, TestComponent comp, ref TestEvent3 args) { }
            }
            """;

        await Verifier(code, []);
    }

    [Test]
    [Description("Tests that a subscription using an anonymous delegate is not flagged as elligible for conversion.")]
    public async Task IgnoreAnonymousDelegate()
    {
        const string code = """
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();

                    SubscribeLocalEvent<TestComponent, TestEvent>((u, c, ref _) => OnTest(u, c));
                }

                private void OnTest(EntityUid uid, TestComponent comp) { }
            }
            """;

        await Verifier(code, []);
    }

    [Test]
    [Description("Tests that a subscription in a method containing preprocessor directives is not flagged as elligible for conversion.")]
    public async Task IgnoreWithPreprocessorDirectives()
    {
        const string code = """

            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();

            #if DEBUG
                    SubscribeLocalEvent<TestComponent, TestEvent>(OnTest);
            #else
                    SubscribeLocalEvent<TestComponent, TestEvent>(OnTest2);
            #endif
                }

                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args) { }
                private void OnTest2(EntityUid uid, TestComponent comp, ref TestEvent args) { }
            }
            """;

        await Verifier(code, []);
    }

    [Test]
    [Description("Tests that a subscription using a generic type parameter is not flagged as elligible for conversion.")]
    public async Task IgnoreWithGenericComponent()
    {
        const string code = """

            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem<TComp> : EntitySystem
                where TComp : Component
            {
                public override void Initialize()
                {
                    base.Initialize();

                    SubscribeLocalEvent<TComp, TestEvent>(OnTest);
                }

                private void OnTest(EntityUid uid, TComp comp, ref TestEvent args) { }
            }
            """;

        await Verifier(code, []);
    }

    [Test]
    [Description("Tests that subscriptions using generic methods as event handlers are not flagged as elligible for conversion.")]
    public async Task IgnoreWithGenericHandler()
    {
        const string code = """

            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();

                    SubscribeLocalEvent<TestComponent, TestEventClassA>(OnTest);
                    SubscribeLocalEvent<TestComponent, TestEventClassB>(OnTest);
                }

                private void OnTest<T>(EntityUid uid, TestComponent comp, ref T args) where T : TestEventArgs { }
            }

            public class TestEventArgs;
            public sealed class TestEventClassA : TestEventArgs;
            public sealed class TestEventClassB : TestEventArgs;
            """;

        await Verifier(code, []);
    }

    [Test]
    [Description("Tests that subscriptions within if statement blocks are not flagged as elligible for conversion.")]
    public async Task IgnoreWithIfStatement()
    {
        const string code = """

            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();

                    if (true)
                        SubscribeLocalEvent<TestComponent, TestEvent>(OnTest);
                    else
                        SubscribeLocalEvent<TestComponent, TestEvent>(OnTest2);
                }

                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args) { }
                private void OnTest2(EntityUid uid, TestComponent comp, ref TestEvent args) { }
            }
            """;

        await Verifier(code, []);
    }
}
