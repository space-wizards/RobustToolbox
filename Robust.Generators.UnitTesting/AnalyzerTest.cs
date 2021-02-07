using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Robust.Generators.UnitTesting
{
    [Parallelizable]
    public abstract class AnalyzerTest
    {
        protected static Assembly GetAssemblyFromCompilation(Compilation newComp)
        {
            using var stream = new MemoryStream();
            newComp.Emit(stream);
            var assembly = Assembly.Load(stream.ToArray());
            return assembly;
        }

        protected static Compilation CreateCompilation(string source)
        {
            var dd = typeof(Enumerable).GetTypeInfo().Assembly.Location;
            var coreDir = Directory.GetParent(dd) ?? throw new Exception("Couldn't find location of coredir");

            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Dictionary<,>).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(YamlFieldAttribute).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar + "mscorlib.dll"),
                MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar +
                                                 "System.Runtime.dll"),
                MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar +
                                                 "System.Collections.dll"),
            };

            var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
            return CSharpCompilation.Create(
                "comp",
                new[] {syntaxTree},
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        protected static (Compilation, ImmutableArray<Diagnostic> diagnostics) RunGenerators(Compilation c,
            params ISourceGenerator[] gens)
        {
            var driver = CSharpGeneratorDriver.Create(
                ImmutableArray.Create(gens),
                ImmutableArray<AdditionalText>.Empty,
                (CSharpParseOptions) c.SyntaxTrees.First().Options);
            driver.RunGeneratorsAndUpdateCompilation(c, out var d, out var diagnostics);
            return (d, diagnostics);
        }
    }
}
