using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.AfterAutoHandleStateAnalyzer,
        Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture, TestOf(typeof(AfterAutoHandleStateAnalyzer))]
public sealed class AfterAutoHandleStateAnalyzerTest
{
    private const string SubscribeEventDef = """
        using System;
        namespace Robust.Shared.GameObjects;

        public readonly struct EntityUid;

        public abstract class EntitySystem
        {
            public void SubscribeLocalEvent<T, TEvent>() where TEvent : notnull { }
        }

        public interface IComponent;
        public interface IComponentState;
        """;

    // A rare case for block-scoped namespace, I thought. Then I realized this
    // only needed the one type definition.
    private const string OtherTypeDefs = """
        using System;

        namespace JetBrains.Annotations
        {
            public sealed class BaseTypeRequiredAttribute(Type baseType) : Attribute;
        }
        """;

    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<AfterAutoHandleStateAnalyzer, DefaultVerifier>
        {
            TestState = { Sources = { code } }
        };

        TestHelper.AddEmbeddedSources(test.TestState,
            "Robust.Shared.Analyzers.ComponentNetworkGeneratorAuxiliary.cs",
            "Robust.Shared.GameObjects.EventBusAttributes.cs");

        test.TestState.Sources.Add(("EntitySystem.Subscriptions.cs", SubscribeEventDef));
        test.TestState.Sources.Add(("Types.cs", OtherTypeDefs));

        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    [Test]
    public async Task Test()
    {
        const string code = """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            [AutoGenerateComponentState(true)]
            public sealed class AutoGenTrue;
            [AutoGenerateComponentState(true, true)]
            public sealed class AutoGenTrueTrue;

            public sealed class NotAutoGen;
            [AutoGenerateComponentState]
            public sealed class AutoGenNoArgs;
            [AutoGenerateComponentState(false)]
            public sealed class AutoGenFalse;
            [AutoGenerateComponentState(RaiseAfterAutoHandleState = true)]
            public sealed class AutoGenIncorrectConstructorArg;

            public sealed class Foo : EntitySystem
            {
                public void Good()
                {
                    // Subscribing to other events works
                    SubscribeLocalEvent<AutoGenNoArgs, object>();
                    // First arg true allows subscribing
                    SubscribeLocalEvent<AutoGenTrue, AfterAutoHandleStateEvent>();
                    SubscribeLocalEvent<AutoGenTrueTrue, AfterAutoHandleStateEvent>();
                }

                public void Bad()
                {
                    // Can't subscribe if AutoGenerateComponentState isn't even present
                    SubscribeLocalEvent<NotAutoGen, AfterAutoHandleStateEvent>();

                    // Can't subscribe if first arg is not specified/false
                    SubscribeLocalEvent<AutoGenNoArgs, AfterAutoHandleStateEvent>();
                    SubscribeLocalEvent<AutoGenFalse, AfterAutoHandleStateEvent>();

                    // Can't subscribe with RaiseAfterAutoHandleState = true because that's
                    // secretly a no-op.
                    SubscribeLocalEvent<AutoGenIncorrectConstructorArg, AfterAutoHandleStateEvent>();
                }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(31,9): error RA0040: Tried to subscribe to AfterAutoHandleStateEvent for 'NotAutoGen' which doesn't have an AutoGenerateComponentState attribute
            VerifyCS.Diagnostic(AfterAutoHandleStateAnalyzer.MissingAttribute).WithSpan(31, 9, 31, 69).WithArguments("NotAutoGen"),
            // /0/Test0.cs(34,9): error RA0041: Tried to subscribe to AfterAutoHandleStateEvent for 'AutoGenNoArgs' which doesn't have RaiseAfterAutoHandleState set
            VerifyCS.Diagnostic(AfterAutoHandleStateAnalyzer.MissingAttributeParam).WithSpan(34, 9, 34, 72).WithArguments("AutoGenNoArgs"),
            // /0/Test0.cs(35,9): error RA0041: Tried to subscribe to AfterAutoHandleStateEvent for 'AutoGenFalse' which doesn't have RaiseAfterAutoHandleState set
            VerifyCS.Diagnostic(AfterAutoHandleStateAnalyzer.MissingAttributeParam).WithSpan(35, 9, 35, 71).WithArguments("AutoGenFalse"),
            // /0/Test0.cs(39,9): error RA0041: Tried to subscribe to AfterAutoHandleStateEvent for 'AutoGenIncorrectConstructorArg' which doesn't have RaiseAfterAutoHandleState set
            VerifyCS.Diagnostic(AfterAutoHandleStateAnalyzer.MissingAttributeParam).WithSpan(39, 9, 39, 89).WithArguments("AutoGenIncorrectConstructorArg")        );
    }
}
