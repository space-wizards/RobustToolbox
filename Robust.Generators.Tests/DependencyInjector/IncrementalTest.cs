using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Robust.Generators.DependencyInjector;

namespace Robust.Generators.Tests.DependencyInjector;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public sealed class IncrementalTest
{
    private const string InputTextFirst = """
        using Robust.Shared.IoC;

        namespace Baz;

        public sealed partial class FooBar
        {
            [Dependency]
            public string Dep;
        }
        """;

    private const string InputTextSecond = """
        using Robust.Shared.IoC;

        namespace Baz;

        public sealed partial class FooBed
        {
            [Dependency]
            public string Dep;
        }
        """;

    [Test]
    public void Test()
    {
        // Create an instance of the source generator.
        var generator = new DependencyInjectorGenerator();

        // Source generators should be tested using 'GeneratorDriver'.
        var driver = CSharpGeneratorDriver.Create(
            new[] { generator.AsSourceGenerator() },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, true));

        // We need to create a compilation with the required source code.
        var compilation = CSharpCompilation.Create(nameof(BasicTest),
            new[] { DependencyTestShared.TypeDefinitionsSyntax, CSharpSyntaxTree.ParseText(InputTextFirst) },
            new[]
            {
                // To support 'System.Attribute' inheritance, add reference to 'System.Private.CoreLib'.
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            });

        // Run generators and retrieve all results.
        var runResult = driver.RunGenerators(compilation).GetRunResult();

        compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(InputTextSecond));

        var runResult2 = driver.RunGenerators(compilation).GetRunResult();

    }
}
