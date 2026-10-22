using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Robust.Analyzers.Tests;

public sealed class RTAnalyzerTest<TAnalyzer> : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    protected override ParseOptions CreateParseOptions()
    {
        var baseOptions = (CSharpParseOptions) base.CreateParseOptions();
        return baseOptions.WithPreprocessorSymbols("ROBUST_ANALYZERS_TEST");
    }
}
