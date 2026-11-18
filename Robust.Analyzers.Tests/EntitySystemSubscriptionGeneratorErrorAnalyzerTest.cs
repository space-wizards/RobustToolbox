extern alias EntitySystemSubscriptionsGenerator;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using Analyzer = EntitySystemSubscriptionsGenerator::Robust.Shared.EntitySystemSubscriptionsGenerator.EntitySystemSubscriptionGeneratorErrorAnalyzer;
using Diagnostics = EntitySystemSubscriptionsGenerator::Robust.Roslyn.Shared.Diagnostics;

namespace Robust.Analyzers.Tests;

[TestFixture]
[TestOf(typeof(Analyzer))]
[Parallelizable(ParallelScope.All)]
public sealed class EntitySystemSubscriptionGeneratorErrorAnalyzerTest
{
    private const string TestTypeDefs = """
        #nullable enable
        global using System;
        global using Robust.Shared.Analyzers;
        global using Robust.Shared.GameObjects;

        namespace Robust.Shared.Analyzers
        {
            [AttributeUsage(AttributeTargets.Method)]
            public sealed class SubscribeLocalEventAttribute : Attribute;
        }

        namespace Robust.Shared.GameObjects
        {
            public readonly struct EntityUid;
            public interface IComponent;
            public readonly struct Entity<T> where T : IComponent?;
            public abstract class EntitySystem;
        }

        public sealed class TestComponent : IComponent;
        public sealed class TestEvent;
        """;

    [Test]
    public async Task NonNullableComponentParameter()
    {
        await Verify("""
            public sealed partial class TestSystem : EntitySystem
            {
                [SubscribeLocalEvent]
                private void OnEvent(EntityUid uid, TestComponent component, ref TestEvent args)
                {
                }
            }
            """);
    }

    [Test]
    public async Task NonNullableEntityComponent()
    {
        await Verify("""
            public sealed partial class TestSystem : EntitySystem
            {
                [SubscribeLocalEvent]
                private void OnEvent(Entity<TestComponent> entity, ref TestEvent args)
                {
                }
            }
            """);
    }

    [Test]
    public async Task NullableComponentParameter()
    {
        await Verify("""
            public sealed partial class TestSystem : EntitySystem
            {
                [SubscribeLocalEvent]
                private void {|#0:OnEvent|}(EntityUid uid, TestComponent? component, ref TestEvent args)
                {
                }
            }
            """, new DiagnosticResult(
                Diagnostics.IdInvalidAMethodSignatureForGeneratedSubscription,
                DiagnosticSeverity.Error).WithLocation(0));
    }

    [Test]
    public async Task NullableEntityComponent()
    {
        await Verify("""
            public sealed partial class TestSystem : EntitySystem
            {
                [SubscribeLocalEvent]
                private void {|#0:OnEvent|}(Entity<TestComponent?> entity, ref TestEvent args)
                {
                }
            }
            """, new DiagnosticResult(
                Diagnostics.IdInvalidAMethodSignatureForGeneratedSubscription,
                DiagnosticSeverity.Error).WithLocation(0));
    }

    private static Task Verify(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<Analyzer, DefaultVerifier>
        {
            TestState =
            {
                Sources = { source },
            },
        };

        test.TestState.Sources.Add(("TestTypeDefs.cs", TestTypeDefs));
        test.TestState.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }
}
