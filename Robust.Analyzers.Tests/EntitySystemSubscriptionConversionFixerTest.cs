using System.Collections.Generic;
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
            public sealed class SubscribeLocalEventAttribute : Attribute;
            public sealed class SubscribeNetworkEventAttribute : Attribute;
            public sealed class SubscribeAllEventAttribute : Attribute;

            public readonly struct EntityUid;

            public delegate void ComponentEventRefHandler<in TComp, TEvent>(EntityUid uid, TComp component, ref TEvent args)
                where TComp : IComponent
                where TEvent : notnull;
            public delegate void EntityEventHandler<in T>(T ev);

            public abstract class EntitySystem
            {
                public virtual void Initialize() { }
                public void SubscribeLocalEvent<TComp, TEvent>(
                    ComponentEventRefHandler<TComp, TEvent> handler)
                    where TComp : IComponent
                    where TEvent : notnull
                { }
                protected void SubscribeNetworkEvent<T>(
                    EntityEventHandler<T> handler,
                    Type[]? before = null, Type[]? after = null)
                    where T : notnull
                { }
            }
        }

        public readonly struct TestEvent;
        public sealed partial class TestComponent : IComponent;
        public sealed class TestNetworkEvent;
    """;

    [Test]
    [Description("Tests that a SubscribeLocalEvent invocation is correctly converted to an attribute.")]
    public async Task ConvertLocalEvent()
    {
        const string code = """
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
            // /0/Test0.cs(9,9): info RA0057: Initialize-based event subscription can be converted to attribute-based
            VerifyCS.Diagnostic().WithSpan(9, 9, 9, 62)
        );
    }

    [Test]
    [Description("Tests that a class that isn't marked partial is given the partial modifier when converted.")]
    public async Task ConvertLocalEvent_AddPartial()
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
            // /0/Test0.cs(9,9): info RA0057: Initialize-based event subscription can be converted to attribute-based
            VerifyCS.Diagnostic().WithSpan(9, 9, 9, 62)
        );
    }

    [Test]
    [Description("Tests that a SubscribeNetworkEvent invocation is correctly converted to an attribute.")]
    public async Task ConvertNetworkEvent()
    {
        const string code = """
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
            // /0/Test0.cs(9,9): info RA0057: Initialize-based event subscription can be converted to attribute-based
            VerifyCS.Diagnostic().WithSpan(9, 9, 9, 56)
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
            // /0/Test0.cs(9,9): info RA0057: Initialize-based event subscription can be converted to attribute-based
            VerifyCS.Diagnostic().WithSpan(9, 9, 9, 62)
        );
    }
}
