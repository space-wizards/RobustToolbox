using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.EntitySystemNameAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
public sealed class EntitySystemNameAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
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

        var test = new CSharpAnalyzerTest<EntitySystemNameAnalyzer, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { code },
                // Include our extra project.
                AdditionalProjects = { {"SharedTestProject", typeDefsProjectState} },
                // We also need our main project to reference the extra project.
                AdditionalProjectReferences = { "SharedTestProject" }
            },

        };

        TestHelper.AddEmbeddedSources(
            test.TestState,
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
    [Description("Checks that an assembly without AssemblyNameAttribute is ignored by the analyzer.")]
    public async Task UnmarkedAssembly()
    {
        const string code = /* lang=c#-test */ """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            public sealed class SharedFooSystem : EntitySystem;
            public sealed class ServerBarSystem : EntitySystem;
            public sealed class ClientBazSystem : EntitySystem;
            """;

        await Verifier(code); // No diagnostics
    }

    [Test]
    [Description("Checks that the analyzer correctly flags diagnostics in a Shared assembly.")]
    public async Task SharedAssembly()
    {
        const string code = /* lang=c#-test */ """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            [assembly: AssemblySide(AssemblySide.Shared)]
            public sealed class SharedFooSystem : EntitySystem;
            public sealed class ServerBarSystem : EntitySystem;
            public sealed class ClientBazSystem : EntitySystem;

            public sealed class SharedTestSystem : TestSystem;
            public sealed class ServerTestSystem : TestSystem;
            public sealed class ClientTestSystem : TestSystem;
            """;

        await Verifier(code,
            // /0/Test0.cs(5,21): warning RA0059: Naming rule violation: SharedFooSystem should be named FooSystem
            VerifyCS.Diagnostic().WithSpan(5, 21, 5, 36).WithArguments("SharedFooSystem", "FooSystem"),
            // /0/Test0.cs(6,21): warning RA0059: Naming rule violation: ServerBarSystem should be named BarSystem
            VerifyCS.Diagnostic().WithSpan(6, 21, 6, 36).WithArguments("ServerBarSystem", "BarSystem"),
            // /0/Test0.cs(7,21): warning RA0059: Naming rule violation: ClientBazSystem should be named BazSystem
            VerifyCS.Diagnostic().WithSpan(7, 21, 7, 36).WithArguments("ClientBazSystem", "BazSystem"),
            // /0/Test0.cs(9,21): warning RA0059: Naming rule violation: SharedTestSystem should be named TestSystem
            VerifyCS.Diagnostic().WithSpan(9, 21, 9, 37).WithArguments("SharedTestSystem", "TestSystem"),
            // /0/Test0.cs(10,21): warning RA0059: Naming rule violation: ServerTestSystem should be named TestSystem
            VerifyCS.Diagnostic().WithSpan(10, 21, 10, 37).WithArguments("ServerTestSystem", "TestSystem"),
            // /0/Test0.cs(11,21): warning RA0059: Naming rule violation: ClientTestSystem should be named TestSystem
            VerifyCS.Diagnostic().WithSpan(11, 21, 11, 37).WithArguments("ClientTestSystem", "TestSystem")
        );
    }

    [Test]
    [Description("Checks that the analyzer correctly flags diagnostics in a Server assembly.")]
    public async Task ServerAssembly()
    {
        const string code = /* lang=c#-test */ """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            [assembly: AssemblySide(AssemblySide.Server)]
            public sealed class SharedFooSystem : EntitySystem;
            public sealed class ServerBarSystem : EntitySystem;
            public sealed class ClientBazSystem : EntitySystem;

            public sealed class SharedTestSystem : TestSystem;
            public sealed class ServerTestSystem : TestSystem;
            public sealed class ClientTestSystem : TestSystem;

            public sealed class ThingSystem : SharedThingSystem;
            """;

        await Verifier(code,
            // /0/Test0.cs(5,21): warning RA0059: Naming rule violation: SharedFooSystem should be named FooSystem
            VerifyCS.Diagnostic().WithSpan(5, 21, 5, 36).WithArguments("SharedFooSystem", "FooSystem"),
            // /0/Test0.cs(6,21): warning RA0059: Naming rule violation: ServerBarSystem should be named BarSystem
            VerifyCS.Diagnostic().WithSpan(6, 21, 6, 36).WithArguments("ServerBarSystem", "BarSystem"),
            // /0/Test0.cs(7,21): warning RA0059: Naming rule violation: ClientBazSystem should be named BazSystem
            VerifyCS.Diagnostic().WithSpan(7, 21, 7, 36).WithArguments("ClientBazSystem", "BazSystem"),
            // /0/Test0.cs(9,21): warning RA0059: Naming rule violation: SharedTestSystem should be named ServerTestSystem
            VerifyCS.Diagnostic().WithSpan(9, 21, 9, 37).WithArguments("SharedTestSystem", "ServerTestSystem"),
            // /0/Test0.cs(11,21): warning RA0059: Naming rule violation: ClientTestSystem should be named ServerTestSystem
            VerifyCS.Diagnostic().WithSpan(11, 21, 11, 37).WithArguments("ClientTestSystem", "ServerTestSystem"),
            // /0/Test0.cs(13,21): warning RA0059: Naming rule violation: ThingSystem should be named ServerThingSystem
            VerifyCS.Diagnostic().WithSpan(13, 21, 13, 32).WithArguments("ThingSystem", "ServerThingSystem")
        );
    }

    [Test]
    [Description("Checks that the analyzer correctly flags diagnostics in a Client assembly.")]
    public async Task ClientAssembly()
    {
        const string code = /* lang=c#-test */ """
            using Robust.Shared.Analyzers;
            using Robust.Shared.GameObjects;

            [assembly: AssemblySide(AssemblySide.Client)]
            public sealed class SharedFooSystem : EntitySystem;
            public sealed class ServerBarSystem : EntitySystem;
            public sealed class ClientBazSystem : EntitySystem;

            public sealed class SharedTestSystem : TestSystem;
            public sealed class ServerTestSystem : TestSystem;
            public sealed class ClientTestSystem : TestSystem;

            public sealed class ThingSystem : SharedThingSystem;
            """;

        await Verifier(code,
            // /0/Test0.cs(5,21): warning RA0059: Naming rule violation: SharedFooSystem should be named FooSystem
            VerifyCS.Diagnostic().WithSpan(5, 21, 5, 36).WithArguments("SharedFooSystem", "FooSystem"),
            // /0/Test0.cs(6,21): warning RA0059: Naming rule violation: ServerBarSystem should be named BarSystem
            VerifyCS.Diagnostic().WithSpan(6, 21, 6, 36).WithArguments("ServerBarSystem", "BarSystem"),
            // /0/Test0.cs(7,21): warning RA0059: Naming rule violation: ClientBazSystem should be named BazSystem
            VerifyCS.Diagnostic().WithSpan(7, 21, 7, 36).WithArguments("ClientBazSystem", "BazSystem"),
            // /0/Test0.cs(9,21): warning RA0059: Naming rule violation: SharedTestSystem should be named ClientTestSystem
            VerifyCS.Diagnostic().WithSpan(9, 21, 9, 37).WithArguments("SharedTestSystem", "ClientTestSystem"),
            // /0/Test0.cs(10,21): warning RA0059: Naming rule violation: ServerTestSystem should be named ClientTestSystem
            VerifyCS.Diagnostic().WithSpan(10, 21, 10, 37).WithArguments("ServerTestSystem", "ClientTestSystem"),
            // /0/Test0.cs(13,21): warning RA0059: Naming rule violation: ThingSystem should be named ClientThingSystem
            VerifyCS.Diagnostic().WithSpan(13, 21, 13, 32).WithArguments("ThingSystem", "ClientThingSystem")
        );
    }
}
