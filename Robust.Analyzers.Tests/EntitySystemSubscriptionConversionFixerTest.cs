using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.EntitySystemSubscriptionConversionAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

public sealed class EntitySystemSubscriptionConversionFixerTest
{
    private static Task Verifier(string code, string fixedCode, params DiagnosticResult[] expected)
    {
        var test = new CSharpCodeFixTest<EntitySystemSubscriptionConversionAnalyzer, EntitySystemSubscriptionConversionFixer, DefaultVerifier>()
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

        test.TestState.Sources.Add(("TestTypeDefs.cs", TestTypeDefs));
        test.FixedState.Sources.Add(("TestTypeDefs.cs", TestTypeDefs));

        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    private static Task Verifier(string[] code, string[] fixedCode, params DiagnosticResult[] expected)
    {
        var test = new CSharpCodeFixTest<EntitySystemSubscriptionConversionAnalyzer, EntitySystemSubscriptionConversionFixer, DefaultVerifier>();

        foreach (var file in code)
        {
            test.TestState.Sources.Add(file);
        }
        foreach (var file in fixedCode)
        {
            test.FixedState.Sources.Add(file);
        }

        test.TestState.Sources.Add(("TestTypeDefs.cs", TestTypeDefs));
        test.FixedState.Sources.Add(("TestTypeDefs.cs", TestTypeDefs));

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
            public delegate void EntityEventHandler<in T>(T ev);
            public delegate void EntitySessionEventHandler<in T>(T msg, string foo);

            public interface IEntitySystem;
            public abstract class EntitySystem : IEntitySystem
            {
                public virtual void Initialize() { }
                public void SubscribeLocalEvent<TComp, TEvent>(
                    ComponentEventRefHandler<TComp, TEvent> handler,
                    Type[]? before = null, Type[]? after = null)
                    where TComp : IComponent
                    where TEvent : notnull
                { }
                protected void SubscribeNetworkEvent<T>(
                    EntityEventHandler<T> handler,
                    Type[]? before = null, Type[]? after = null)
                    where T : notnull
                { }
                protected void SubscribeAllEvent<T>(
                    EntitySessionEventHandler<T> handler,
                    Type[]? before = null, Type[]? after = null)
                    where T : notnull
                { }
            }
        }

        namespace Robust.Shared.Analyzers
        {
            public sealed class SubscribeLocalEventAttribute(Type[]? before = null, Type[]? after = null) : Attribute;
            public sealed class SubscribeNetworkEventAttribute(Type[]? before = null, Type[]? after = null) : Attribute;
            public sealed class EventSubscriptionAttribute(Type[]? before = null, Type[]? after = null) : Attribute;
        }

        public readonly struct TestEvent;
        public readonly struct TestEvent2;
        public sealed partial class TestComponent : IComponent;
        public sealed class TestNetworkEvent;

        public sealed class SomeOtherSystemA : EntitySystem;
        public sealed class SomeOtherSystemB : EntitySystem;
    """;

    [Test]
    [Description("Tests that a SubscribeLocalEvent invocation is correctly converted to an attribute.")]
    public async Task ConvertLocalEvent()
    {
        const string code = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();

                    SubscribeLocalEvent<TestComponent, TestEvent>(OnTest); // Comment here
                }

                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }
            }
            """;

        const string fixedCode = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();
                }

                [SubscribeLocalEvent]
                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }
            }
            """;

        await Verifier(code, fixedCode,
            // /0/Test0.cs(10,9): info RA0058: Event subscription using SubscribeLocalEvent can be converted to use SubscribeLocalEventAttribute
            VerifyCS.Diagnostic().WithSpan(10, 9, 10, 62).WithArguments("SubscribeLocalEvent", "SubscribeLocalEventAttribute")
        );
    }

    [Test]
    [Description("Tests that multiple SubscribeLocalEvent invocations are correctly converted to attributes.")]
    public async Task ConvertLocalEvent_Multiple()
    {
        const string code = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();

                    SubscribeLocalEvent<TestComponent, TestEvent>(OnTest); // Comment here
                    SubscribeLocalEvent<TestComponent, TestEvent2>(OnTest2);
                }

                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }

                private void OnTest2(EntityUid uid, TestComponent comp, ref TestEvent2 args)
                {
                    // Do something
                }
            }
            """;

        const string fixedCode = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();
                }

                [SubscribeLocalEvent]
                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }

                [SubscribeLocalEvent]
                private void OnTest2(EntityUid uid, TestComponent comp, ref TestEvent2 args)
                {
                    // Do something
                }
            }
            """;

        await Verifier(code, fixedCode,
            // /0/Test0.cs(10,9): info RA0057: Event subscription using SubscribeLocalEvent can be converted to use SubscribeLocalEventAttribute
            VerifyCS.Diagnostic().WithSpan(10, 9, 10, 62).WithArguments("SubscribeLocalEvent", "SubscribeLocalEventAttribute"),
            // /0/Test0.cs(11,9): info RA0057: Event subscription using SubscribeLocalEvent can be converted to use SubscribeLocalEventAttribute
            VerifyCS.Diagnostic().WithSpan(11, 9, 11, 64).WithArguments("SubscribeLocalEvent", "SubscribeLocalEventAttribute")
        );
    }

    [Test]
    [Description("Tests that multiple SubscribeLocalEvent invocations are correctly converted to attributes when there is a gap between them.")]
    public async Task ConvertLocalEvent_MultipleWithGap()
    {
        const string code = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();

                    SubscribeLocalEvent<TestComponent, TestEvent>(OnTest); // Comment here

                    SubscribeLocalEvent<TestComponent, TestEvent2>(OnTest2);
                }

                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }

                private void OnTest2(EntityUid uid, TestComponent comp, ref TestEvent2 args)
                {
                    // Do something
                }
            }
            """;

        const string fixedCode = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();
                }

                [SubscribeLocalEvent]
                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }

                [SubscribeLocalEvent]
                private void OnTest2(EntityUid uid, TestComponent comp, ref TestEvent2 args)
                {
                    // Do something
                }
            }
            """;

        await Verifier(code, fixedCode,
            // /0/Test0.cs(10,9): info RA0057: Event subscription using SubscribeLocalEvent can be converted to use SubscribeLocalEventAttribute
            VerifyCS.Diagnostic().WithSpan(10, 9, 10, 62).WithArguments("SubscribeLocalEvent", "SubscribeLocalEventAttribute"),
            // /0/Test0.cs(12,9): info RA0057:Event subscription using SubscribeLocalEvent can be converted to use SubscribeLocalEventAttribute
            VerifyCS.Diagnostic().WithSpan(12, 9, 12, 64).WithArguments("SubscribeLocalEvent", "SubscribeLocalEventAttribute")
        );
    }

    [Test]
    [Description("Tests that a SubscribeLocalEvent invocation with before and after parameters is correctly converted to an attribute.")]
    public async Task ConvertLocalEvent_WithBeforeAfter()
    {
        const string code = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();

                    SubscribeLocalEvent<TestComponent, TestEvent>(OnTest, before: [typeof(SomeOtherSystemA)], after: new[] { typeof(SomeOtherSystemB) });
                }

                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }
            }
            """;

        const string fixedCode = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();
                }

                [SubscribeLocalEvent(before: [typeof(SomeOtherSystemA)], after: [typeof(SomeOtherSystemB)])]
                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }
            }
            """;

        await Verifier(code, fixedCode,
            // /0/Test0.cs(10,9): info RA0058: Event subscription using SubscribeLocalEvent can be converted to use SubscribeLocalEventAttribute
            VerifyCS.Diagnostic().WithSpan(10, 9, 10, 141).WithArguments("SubscribeLocalEvent", "SubscribeLocalEventAttribute")
        );
    }

    [Test]
    [Description("Tests that a SubscribeLocalEvent invocation a before parameter is correctly converted to an attribute.")]
    public async Task ConvertLocalEvent_WithBefore()
    {
        const string code = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();

                    SubscribeLocalEvent<TestComponent, TestEvent>(OnTest, before: new[] { typeof(SomeOtherSystemA) });
                }

                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }
            }
            """;

        const string fixedCode = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();
                }

                [SubscribeLocalEvent(before: [typeof(SomeOtherSystemA)])]
                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }
            }
            """;

        await Verifier(code, fixedCode,
            // /0/Test0.cs(10,9): info RA0058: Event subscription using SubscribeLocalEvent can be converted to use SubscribeLocalEventAttribute
            VerifyCS.Diagnostic().WithSpan(10, 9, 10, 106).WithArguments("SubscribeLocalEvent", "SubscribeLocalEventAttribute")
        );
    }

    [Test]
    [Description("Tests that a SubscribeLocalEvent invocation an after parameter is correctly converted to an attribute.")]
    public async Task ConvertLocalEvent_WithAfter()
    {
        const string code = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();

                    SubscribeLocalEvent<TestComponent, TestEvent>(OnTest, after: [typeof(SomeOtherSystemA), typeof(SomeOtherSystemB)]);
                }

                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }
            }
            """;

        const string fixedCode = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();
                }

                [SubscribeLocalEvent(after: [typeof(SomeOtherSystemA), typeof(SomeOtherSystemB)])]
                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }
            }
            """;

        await Verifier(code, fixedCode,
            // /0/Test0.cs(10,9): info RA0058: Event subscription using SubscribeLocalEvent can be converted to use SubscribeLocalEventAttribute
            VerifyCS.Diagnostic().WithSpan(10, 9, 10, 123).WithArguments("SubscribeLocalEvent", "SubscribeLocalEventAttribute")
        );
    }

    [Test]
    [Description("Tests that a class that isn't marked partial is given the partial modifier when converted.")]
    public async Task ConvertLocalEvent_AddPartial()
    {
        const string code = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();

                    SubscribeLocalEvent<TestComponent, TestEvent>(OnTest); // Comment here
                }

                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }
            }
            """;

        const string fixedCode = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();
                }

                [SubscribeLocalEvent]
                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }
            }
            """;

        await Verifier(code, fixedCode,
            // /0/Test0.cs(10,9): info RA0057: Event subscription using SubscribeLocalEvent can be converted to use SubscribeLocalEventAttribute
            VerifyCS.Diagnostic().WithSpan(10, 9, 10, 62).WithArguments("SubscribeLocalEvent", "SubscribeLocalEventAttribute")
        );
    }

    [Test]
    [Description("Tests that a class is given the using directive for the SubscribeLocalEventAttribute namespace when converted.")]
    public async Task ConvertLocalEvent_AddUsingDirective()
    {
        const string code = """
            using Robust.Shared.GameObjects;

            public sealed class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();

                    SubscribeLocalEvent<TestComponent, TestEvent>(OnTest); // Comment here
                }

                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }
            }
            """;

        const string fixedCode = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();
                }

                [SubscribeLocalEvent]
                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }
            }
            """;

        await Verifier(code, fixedCode,
            // /0/Test0.cs(9,9): info RA0057: Event subscription using SubscribeLocalEvent can be converted to use SubscribeLocalEventAttribute
            VerifyCS.Diagnostic().WithSpan(9, 9, 9, 62).WithArguments("SubscribeLocalEvent", "SubscribeLocalEventAttribute")
        );
    }

    [Test]
    [Description("Tests that the conversion isn't confused by other methods with the same name as the event handler.")]
    public async Task ConvertLocalEvent_HandlerOverload()
    {
        const string code = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();

                    SubscribeLocalEvent<TestComponent, TestEvent>(OnTest); // Comment here
                }

                private void OnTest(string foo) { }

                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }
            }
            """;

        const string fixedCode = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();
                }

                private void OnTest(string foo) { }

                [SubscribeLocalEvent]
                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }
            }
            """;

        await Verifier(code, fixedCode,
            // /0/Test0.cs(10,9): info RA0057: Event subscription using SubscribeLocalEvent can be converted to use SubscribeLocalEventAttribute
            VerifyCS.Diagnostic().WithSpan(10, 9, 10, 62).WithArguments("SubscribeLocalEvent", "SubscribeLocalEventAttribute")
        );
    }

    [Test]
    [Description("Tests that a SubscribeNetworkEvent invocation is correctly converted to an attribute.")]
    public async Task ConvertNetworkEvent()
    {
        const string code = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();

                    SubscribeNetworkEvent<TestNetworkEvent>(OnTest); // Comment here
                }

                private void OnTest(TestNetworkEvent args)
                {
                    // Do something
                }
            }
            """;

        const string fixedCode = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();
                }

                [SubscribeNetworkEvent]
                private void OnTest(TestNetworkEvent args)
                {
                    // Do something
                }
            }
            """;

        await Verifier(code, fixedCode,
            // /0/Test0.cs(10,9): info RA0058: Event subscription using SubscribeNetworkEvent can be converted to use SubscribeNetworkEventAttribute
            VerifyCS.Diagnostic().WithSpan(10, 9, 10, 56).WithArguments("SubscribeNetworkEvent", "SubscribeNetworkEventAttribute")
        );
    }

    [Test]
    [Description("Tests that a SubscribeAllEvent invocation is correctly converted to an attribute.")]
    public async Task ConvertAllEvent()
    {
        const string code = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();

                    SubscribeAllEvent<TestNetworkEvent>(OnTest); // Comment here
                }

                private void OnTest(TestNetworkEvent args, string foo)
                {
                    // Do something
                }
            }
            """;

        const string fixedCode = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();
                }

                [EventSubscription]
                private void OnTest(TestNetworkEvent args, string foo)
                {
                    // Do something
                }
            }
            """;

        await Verifier(code, fixedCode,
            // /0/Test0.cs(10,9): info RA0058: Event subscription using SubscribeAllEvent can be converted to use EventSubscriptionAttribute
            VerifyCS.Diagnostic().WithSpan(10, 9, 10, 52).WithArguments("SubscribeAllEvent", "EventSubscriptionAttribute")
        );
    }

    [Test]
    [Description("Tests that the conversion works correctly when the Initialize and event handler methods are declared in separate files (partial classes).")]
    public async Task ConvertLocalEvent_WithPartials()
    {
        const string code1 = """
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();

                    SubscribeLocalEvent<TestComponent, TestEvent>(OnTest); // Comment here
                }
            }
            """;

        const string code2 = """
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }
            }
            """;

        const string fixed1 = """
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();
                }
            }
            """;

        const string fixed2 = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed partial class InitalizeBasedSystem : EntitySystem
            {
                [SubscribeLocalEvent]
                private void OnTest(EntityUid uid, TestComponent comp, ref TestEvent args)
                {
                    // Do something
                }
            }
            """;

        await Verifier([code1, code2], [fixed1, fixed2],
            // /0/Test0.cs(9,9): info RA0057: Event subscription using SubscribeLocalEvent can be converted to use SubscribeLocalEventAttribute
            VerifyCS.Diagnostic().WithSpan(9, 9, 9, 62).WithArguments("SubscribeLocalEvent", "SubscribeLocalEventAttribute")
        );
    }
}
