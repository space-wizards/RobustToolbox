using System.Collections.Generic;
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
            using var stream = typeof(AccessAnalyzer_Test).Assembly.GetManifestResourceStream(fileName)!;
            state.Sources.Add((fileName, SourceText.From(stream)));
        }
    }
}
