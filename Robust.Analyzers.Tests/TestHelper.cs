using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace Robust.Analyzers.Tests;

public static class TestHelper
{
    public static void AddEmbeddedSources(SolutionState state, params string[] embeddedFiles)
    {
        AddEmbeddedSources(state, (IEnumerable<string>) embeddedFiles);
    }

    public static void AddEmbeddedSources(SolutionState state, IEnumerable<string> embeddedFiles)
    {
        foreach (var fileName in embeddedFiles)
        {
            state.Sources.Add((fileName, GetEmbeddedFile(fileName)));
        }
    }

    public static IEnumerable<SyntaxTree> GetEmbeddedSyntaxTrees(params string[] embeddedFiles)
    {
        return embeddedFiles.Select(fileName => CSharpSyntaxTree.ParseText(GetEmbeddedFile(fileName)));
    }

    private static SourceText GetEmbeddedFile(string fileName)
    {
        using var stream = typeof(TestHelper).Assembly.GetManifestResourceStream(fileName)!;
        return SourceText.From(stream);
    }
}
