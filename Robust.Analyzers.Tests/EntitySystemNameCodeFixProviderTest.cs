using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.EntitySystemNameAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

public sealed class EntitySystemNameCodeFixProviderTest
{
    private static Task Verifier(
        (string name, string text) code,
        (string name, string text) fixedCode,
        params DiagnosticResult[] expected)
    {
        // Create a second project to contain our EntitySystem and IEntitySystem definitions
        // This puts them in a separate assembly, so we can use AssemblySideAttribute in our
        // test code without it affecting these definitions.
        var typeDefsProjectState = new ProjectState("SharedTestProject", LanguageNames.CSharp, "", "cs")
        {
            Sources =
            {
                TestTypeDefs,
            }
        };

        var test = new CSharpCodeFixTest<EntitySystemNameAnalyzer, EntitySystemNameCodeFixProvider, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { code },
                // Include our extra project.
                AdditionalProjects = { {"SharedTestProject", typeDefsProjectState} },
                // We also need our main project to reference the extra project.
                AdditionalProjectReferences = { "SharedTestProject" }
            },
            FixedState =
            {
                Sources = { fixedCode },
                // Include our extra project.
                AdditionalProjects = { {"SharedTestProject", typeDefsProjectState} },
                // We also need our main project to reference the extra project.
                AdditionalProjectReferences = { "SharedTestProject" }
            }
        };

        TestHelper.AddEmbeddedSources(
            test.TestState,
            "Robust.Shared.Analyzers.AssemblySideAttribute.cs"
        );

        TestHelper.AddEmbeddedSources(
            test.FixedState,
            "Robust.Shared.Analyzers.AssemblySideAttribute.cs"
        );

        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    private const string TestTypeDefs = /* lang=c#-test */ """
        namespace Robust.Shared.GameObjects
        {
            public interface IEntitySystem;
            public abstract class EntitySystem : IEntitySystem;
            public class TestSystem : EntitySystem;
            public class SharedThingSystem : EntitySystem;
        }
    """;

    [Test]
    [Description("")]
    public async Task SharedAssembly()
    {
        const string name = "/0/SharedFooSystem.cs";
        const string code = /* lang=c#-test */ """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            [assembly: AssemblySide(AssemblySide.Shared)]
            public sealed class SharedFooSystem : EntitySystem;
            public sealed class ServerBarSystem : EntitySystem;
            public sealed class ClientBazSystem : EntitySystem;
            """;

        const string fixedName = "/0/FooSystem.cs";
        const string fixedCode = /* lang=c#-test */ """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            [assembly: AssemblySide(AssemblySide.Shared)]
            public sealed class FooSystem : EntitySystem;
            public sealed class BarSystem : EntitySystem;
            public sealed class BazSystem : EntitySystem;
            """;

        await Verifier((name, code), (fixedName, fixedCode),
            // /0/Test0.cs(5,21): warning RA0059: Naming rule violation: SharedFooSystem should be named FooSystem
            VerifyCS.Diagnostic().WithSpan(name, 5, 21, 5, 36).WithArguments("SharedFooSystem", "FooSystem"),
            // /0/Test0.cs(6,21): warning RA0059: Naming rule violation: ServerBarSystem should be named BarSystem
            VerifyCS.Diagnostic().WithSpan(name, 6, 21, 6, 36).WithArguments("ServerBarSystem", "BarSystem"),
            // /0/Test0.cs(7,21): warning RA0059: Naming rule violation: ClientBazSystem should be named BazSystem
            VerifyCS.Diagnostic().WithSpan(name, 7, 21, 7, 36).WithArguments("ClientBazSystem", "BazSystem")
        );
    }

    [Test]
    [Description("")]
    public async Task ClientAssembly()
    {
        const string name = "/0/ClientBazSystem.cs";
        const string code = /* lang=c#-test */ """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            [assembly: AssemblySide(AssemblySide.Client)]
            public sealed class SharedFooSystem : EntitySystem;
            public sealed class ServerBarSystem : EntitySystem;
            public sealed class ClientBazSystem : EntitySystem;

            public sealed class ThingSystem : SharedThingSystem;
            """;

        const string fixedName = "/0/BazSystem.cs";
        const string fixedCode = /* lang=c#-test */ """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            [assembly: AssemblySide(AssemblySide.Client)]
            public sealed class FooSystem : EntitySystem;
            public sealed class BarSystem : EntitySystem;
            public sealed class BazSystem : EntitySystem;

            public sealed class ClientThingSystem : SharedThingSystem;
            """;

        await Verifier((name, code), (fixedName, fixedCode),
            // /0/Test0.cs(5,21): warning RA0059: Naming rule violation: SharedFooSystem should be named FooSystem
            VerifyCS.Diagnostic().WithSpan(name, 5, 21, 5, 36).WithArguments("SharedFooSystem", "FooSystem"),
            // /0/Test0.cs(6,21): warning RA0059: Naming rule violation: ServerBarSystem should be named BarSystem
            VerifyCS.Diagnostic().WithSpan(name, 6, 21, 6, 36).WithArguments("ServerBarSystem", "BarSystem"),
            // /0/Test0.cs(7,21): warning RA0059: Naming rule violation: ClientBazSystem should be named BazSystem
            VerifyCS.Diagnostic().WithSpan(name, 7, 21, 7, 36).WithArguments("ClientBazSystem", "BazSystem"),
            // /0/Test0.cs(9,21): warning RA0059: Naming rule violation: ThingSystem should be named ClientThingSystem
            VerifyCS.Diagnostic().WithSpan(name, 9, 21, 9, 32).WithArguments("ThingSystem", "ClientThingSystem")
        );
    }
}
